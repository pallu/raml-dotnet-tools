﻿namespace Raml.Common
{
	public class RamlChooserActionParams
	{
		public RamlChooserActionParams(string ramlSource, string ramlFilePath, string ramlTitle, string templatesPath, string targetFileName, string targetNamespace, bool? doNotScaffold)
		{
			RamlSource = ramlSource;
			RamlFilePath = ramlFilePath;
			RamlTitle = ramlTitle;
			TemplatesPath = templatesPath;
			TargetFileName = targetFileName;
			TargetNamespace = targetNamespace;
			DoNotScaffold = doNotScaffold;
		}

		public string RamlSource { get; private set; } // the original URL or file path of the RAML
		public string RamlFilePath { get; private set; } // file path of a temporary location of the RAML
		public string RamlTitle { get; private set; }
		public string TemplatesPath { get; private set; }
		public string TargetFileName { get; private set; }
		public string TargetNamespace { get; private set; }
		public bool? DoNotScaffold { get; private set; }
	}
}