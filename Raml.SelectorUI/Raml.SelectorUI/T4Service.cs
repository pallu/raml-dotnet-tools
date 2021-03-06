﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TextTemplating;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using Raml.Tools;
using Raml.Tools.ClientGenerator;

namespace Raml.Common
{
	public class T4Service : IT4Service, ITextTemplatingCallback
	{
		private readonly List<string> messages = new List<string>();
		private string errors = string.Empty;

		public T4Service(IServiceProvider serviceProvider)
		{
			ServiceProvider = serviceProvider;
		}

		internal IServiceProvider ServiceProvider
		{
			get;
			private set;
		}

		public Result TransformText(string templatePath, ClientGeneratorModel model, string path, string ramlFile, string targetNamespace)
		{
			// Get the T4 engine from VS
			var textTemplating = ServiceProvider.GetService(typeof (STextTemplating)) as ITextTemplating;

			messages.Clear();
			textTemplating.BeginErrorSession();

			// Read the T4 from disk into memory
			var templateFileContent = File.ReadAllText(templatePath);
			templateFileContent = templateFileContent.Replace("$(binDir)", path);
			templateFileContent = templateFileContent.Replace("$(ramlFile)", ramlFile.Replace("\\", "\\\\"));
			templateFileContent = templateFileContent.Replace("$(namespace)", targetNamespace);

			// Initialize the T4 host so we can transfer the Dictionary contents
			// into the new app domain in which the host runs
			var host = textTemplating as ITextTemplatingSessionHost;
			host.Session = host.CreateSession();
			host.Session["model"] = model;

			var content = textTemplating.ProcessTemplate(templatePath, templateFileContent, this, null);
			textTemplating.EndErrorSession();

			return new Result { Content = content, HasErrors = content.StartsWith("ErrorGeneratingOutput"), Errors = errors, Messages = messages };
		}

		public Result TransformText<T>(string templatePath, string paramName, T param)
		{
			// Get the T4 engine from VS
			var textTemplating = ServiceProvider.GetService(typeof(STextTemplating)) as ITextTemplating;

			messages.Clear();
			textTemplating.BeginErrorSession();

			// Read the T4 from disk into memory
			var templateFileContent = File.ReadAllText(templatePath);

			// Initialize the T4 host so we can transfer the Dictionary contents
			// into the new app domain in which the host runs
			var host = textTemplating as ITextTemplatingSessionHost;
			host.Session = host.CreateSession();
			host.Session[paramName] = param;

			var content = textTemplating.ProcessTemplate(templatePath, templateFileContent, this, null);
			textTemplating.EndErrorSession();

			return new Result { Content = content, HasErrors = content.StartsWith("ErrorGeneratingOutput"), Errors = errors, Messages = messages };
		}


		public void ErrorCallback(bool warning, string message, int line, int column)
		{
			var s = string.Format("{0}: {1}. Line: {2} Column: {3}", warning ? "warning" : "error", message, line, column);
			errors += "\r\n" + s;
			messages.Add(s);
		}

		public void SetFileExtension(string extension)
		{
		}

		public void SetOutputEncoding(Encoding encoding, bool fromOutputDirective)
		{
		}
	}
}