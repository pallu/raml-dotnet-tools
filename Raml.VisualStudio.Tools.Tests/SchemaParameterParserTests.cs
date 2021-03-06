﻿using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Raml.Parser;
using Raml.Parser.Expressions;
using Raml.Tools.Pluralization;

namespace Raml.Tools.Tests
{
	[TestFixture]
	public class SchemaParameterParserTests
	{
		private Resource searchResource;
		private Resource deliveriesResource;
		private Resource foldersResource;
		private readonly SchemaParameterParser schemaParameterParser = new SchemaParameterParser(new EnglishPluralizationService());

		[SetUp]
		public void Setup()
		{
			var raml = new RamlParser().Load("box.raml");
			var raml2 = new RamlParser().Load("congo-drones-5-f.raml");
			deliveriesResource = raml2.Resources.First(r => r.RelativeUri == "/deliveries");
			searchResource = raml.Resources.First(r => r.RelativeUri == "/search");
			foldersResource = raml.Resources.First(r => r.RelativeUri == "/folders");
		}


		[Test]
		public void ShouldPluralize()
		{
			var actual = schemaParameterParser.Parse("<<resourcePathName | !pluralize>>", searchResource, searchResource.Methods.First());
			Assert.AreEqual("searches", actual);
		}

		[Test]
		public void ShouldPluralize_WhenNoSpace()
		{
			var actual = schemaParameterParser.Parse("<<resourcePathName|!pluralize>>", searchResource, searchResource.Methods.First());
			Assert.AreEqual("searches", actual);
		}

		[Test]
		public void ShouldSingularize_WhenIrregular()
		{
			var actual = schemaParameterParser.Parse("<<resourcePathName | !singularize>>", deliveriesResource, deliveriesResource.Methods.First());
			Assert.AreEqual("delivery", actual);
		}

		[Test]
		public void ShouldSingularize_WhenRegular()
		{
			var actual = schemaParameterParser.Parse("<<resourcePathName | !singularize>>", foldersResource, deliveriesResource.Methods.First());
			Assert.AreEqual("folder", actual);
		}

		[Test]
		public void ShouldSingularize_WhenNoSpace()
		{
			var actual = schemaParameterParser.Parse("<<resourcePathName|!singularize>>", deliveriesResource, deliveriesResource.Methods.First());
			Assert.AreEqual("delivery", actual);
		}

		[Test]
		public void ShouldGetResourcePathName()
		{
			var actual = schemaParameterParser.Parse("<<resourcePathName>>", deliveriesResource, deliveriesResource.Methods.First());
			Assert.AreEqual("deliveries", actual);
		}

		[Test]
		public void ShouldGetResourcePath()
		{
			var actual = schemaParameterParser.Parse("<<resourcePath>>", deliveriesResource, deliveriesResource.Methods.First());
			Assert.AreEqual("/deliveries", actual);
		}

		[Test]
		public void ShouldGetMethod()
		{
			var actual = schemaParameterParser.Parse("<<methodName>>", deliveriesResource, deliveriesResource.Methods.First());
			Assert.AreEqual("get", actual);
		}

		[Test]
		public void ShouldGetMixedParameter()
		{
			var actual = schemaParameterParser.Parse("<<methodName>>new<<resourcePathName | !singularize>>", deliveriesResource, deliveriesResource.Methods.First());
			Assert.AreEqual("getnewdelivery", actual);
		}
	}
}