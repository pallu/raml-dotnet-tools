﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Raml.Parser.Expressions;

namespace Raml.Tools.ClientGenerator
{
    public class ClientGeneratorService : GeneratorServiceBase
    {
		private readonly ClientMethodsGenerator clientMethodsGenerator;

        private readonly string rootClassName;

        private readonly IDictionary<string, ApiObject> queryObjects = new Dictionary<string, ApiObject>();
        private readonly IDictionary<string, ApiObject> headerObjects = new Dictionary<string, ApiObject>();
        private readonly IDictionary<string, ApiObject> responseHeadersObjects = new Dictionary<string, ApiObject>();


        private ICollection<ClassObject> classes;

        private IDictionary<string, ClassObject> classesObjectsRegistry;
	    

		private readonly ApiRequestObjectsGenerator apiRequestGenerator = new ApiRequestObjectsGenerator();
		private readonly ApiResponseObjectsGenerator apiResponseGenerator = new ApiResponseObjectsGenerator();

	    public ClientGeneratorService(RamlDocument raml, string rootClassName)
            : base(raml)
	    {
		    clientMethodsGenerator = new ClientMethodsGenerator(raml);
            this.rootClassName = rootClassName;
        }

        public ClientGeneratorModel BuildModel()
        {
            warnings = new Dictionary<string, string>();
            classesNames = new Collection<string>();
            classes = new Collection<ClassObject>();
            classesObjectsRegistry = new Dictionary<string, ClassObject>();
			uriParameterObjects = new Dictionary<string, ApiObject>();

            schemaRequestObjects = GetRequestObjects();
            schemaResponseObjects = GetResponseObjects();

            CleanProperties(schemaRequestObjects);
            CleanProperties(schemaResponseObjects);

            var parentClass = new ClassObject { Name = rootClassName, Description = "Main class for grouping root resources. Nested resources are defined as properties. The constructor can optionally receive an URL and HttpClient instance to override the default ones." };
            classesNames.Add(parentClass.Name);
            classes.Add(parentClass);
            classesObjectsRegistry.Add(rootClassName, parentClass);

            var classObjects = GetClasses(raml.Resources, null, parentClass, null);
            SetClassesProperties(classesObjectsRegistry[rootClassName]);

	        var apiRequestObjects = apiRequestGenerator.Generate(classObjects);
			var apiResponseObjects = apiResponseGenerator.Generate(classObjects);

            return new ClientGeneratorModel
                   {
                       Namespace = NetNamingMapper.GetNamespace(raml.Title),
                       RequestObjects = schemaRequestObjects,
                       ResponseObjects = schemaResponseObjects,
                       QueryObjects = queryObjects,
                       HeaderObjects = headerObjects,
					   ApiRequestObjects = apiRequestObjects,
					   ApiResponseObjects = apiResponseObjects,
                       ResponseHeaderObjects = responseHeadersObjects,
                       BaseUriParameters = ParametersMapper.Map(raml.BaseUriParameters),
                       BaseUri = raml.BaseUri,
                       Security = SecurityParser.GetSecurity(raml),
                       Version = raml.Version,
                       Warnings = warnings,
                       Classes = classObjects.Where(c => c.Name != rootClassName).ToArray(),
                       Root = classObjects.First(c => c.Name == rootClassName),
					   UriParameterObjects = uriParameterObjects
                   };
        }





	    private ICollection<ClassObject> GetClasses(IEnumerable<Resource> resources, Resource parent, ClassObject parentClass, string url)
        {
            if (resources == null)
                return classes;

            foreach (var resource in resources)
            {
                if (resource == null)
                    continue;

                var fullUrl = GetUrl(url, resource.RelativeUri);
                // when the resource is a parameter dont generate a class but add it's methods and children to the parent
                if (resource.RelativeUri.StartsWith("/{") && resource.RelativeUri.EndsWith("}"))
                {
	                var generatedMethods = clientMethodsGenerator.GetMethods(resource, fullUrl, parentClass,
		                parentClass.Name, schemaResponseObjects, uriParameterObjects, queryObjects, headerObjects,
		                responseHeadersObjects, schemaRequestObjects);

                    foreach (var method in generatedMethods)
                    {
                        parentClass.Methods.Add(method);
                    }

                    var children = GetClasses(resource.Resources, resource, parentClass, fullUrl);
                    foreach (var child in children)
                    {
                        parentClass.Children.Add(child);
                    }
                    continue;
                }

                var classObj = new ClassObject
                               {
                                   Name = GetUniqueObjectName(resource, parent),
                                   Description = resource.Description
                               };
	            classObj.Methods = clientMethodsGenerator.GetMethods(resource, fullUrl, null, classObj.Name,
		            schemaResponseObjects, uriParameterObjects, queryObjects, headerObjects, responseHeadersObjects, schemaRequestObjects);

                classObj.Children = GetClasses(resource.Resources, resource, classObj, fullUrl);
                classesNames.Add(classObj.Name);
                classes.Add(classObj);
                classesObjectsRegistry.Add(CalculateClassKey(fullUrl), classObj);
            }
            return classes;
        }





	    private void SetClassesProperties(ClassObject rootClassObject)
        {
            var propertiesNames = new List<string>();
            foreach (var parentResource in raml.Resources)
            {
                var fullUrl = GetUrl(null, parentResource.RelativeUri);

                if (!parentResource.RelativeUri.StartsWith("/{") || !parentResource.RelativeUri.EndsWith("}"))
                {
                    var property = new FluentProperty
                                   {
                                       Name = GetExistingObjectName(fullUrl),
                                   };

                    if (propertiesNames.Contains(property.Name))
                        continue;

                    rootClassObject.Properties.Add(property);
                    propertiesNames.Add(property.Name);

                    var classObj = GetClassObject(fullUrl);
                    if (classObj == null)
                        throw new InvalidOperationException("Null class object for resource " + fullUrl);

                    SetFluentApiProperties(parentResource, classObj, fullUrl);
                }
                else
                {
                    SetFluentApiProperties(parentResource, rootClassObject, fullUrl);
                }
                SetClassesProperties(parentResource.Resources, rootClassObject, fullUrl);
            }
        }

        private void SetClassesProperties(IEnumerable<Resource> resources, ClassObject parentClass, string url)
        {
            if (resources == null)
                return;

            foreach (var resource in resources)
            {
                if (resource == null)
                    continue;

                var fullUrl = GetUrl(url, resource.RelativeUri);
                if (!resource.RelativeUri.StartsWith("/{") || !resource.RelativeUri.EndsWith("}"))
                {
                    var classObj = GetClassObject(fullUrl);
                    if (classObj == null)
                        throw new InvalidOperationException("Null class object for resource " + fullUrl);

                    SetFluentApiProperties(resource, classObj, fullUrl);
                }
                else
                {
                    SetFluentApiProperties(resource, parentClass, fullUrl);
                }
                SetClassesProperties(resource.Resources, parentClass, fullUrl);
            }
        }

        private ClassObject GetClassObject(string url)
        {
            var key = CalculateClassKey(url);
            if (!classesObjectsRegistry.ContainsKey(key))
                throw new InvalidOperationException("Could not find class for " + url);

            return classesObjectsRegistry[key];
        }

        private void SetFluentApiProperties(Resource resource, ClassObject classObject, string url)
        {
            var propertiesNames = new List<string>();

            foreach (var childResource in resource.Resources)
            {
                if (childResource.RelativeUri.StartsWith("/{") && childResource.RelativeUri.EndsWith("}"))
                    continue;

                var property = new FluentProperty
                {
                    Name = GetExistingObjectName(GetUrl(url, childResource.RelativeUri)),
                };

                if (propertiesNames.Contains(property.Name))
                    continue;

                classObject.Properties.Add(property);
                propertiesNames.Add(property.Name);
            }
        }

        private string GetExistingObjectName(string url)
        {
            var key = CalculateClassKey(url);
            if (!classesObjectsRegistry.ContainsKey(key))
                throw new InvalidOperationException("Could not found Class for " + url);

            return classesObjectsRegistry[key].Name;
        }


	    public IDictionary<string, ApiObject> QueryObjects
        {
            get { return queryObjects; }
        }
    }
}