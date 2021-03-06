﻿using NUnit.Framework;
using Raml.Tools.WebApiGenerator;

namespace Raml.Tools.Tests
{
	[TestFixture]
	public class UrlGeneratorHelperTests
	{
		[Test]
		public void should_get_relative_uri()
		{
			Assert.AreEqual("users", UrlGeneratorHelper.GetRelativeUri("movies/{id}/users", "/movies/{id}"));
		}

		[Test]
		public void should_replace_consecutive_params()
		{
			Assert.AreEqual("{mediaTypeExtension}/users", UrlGeneratorHelper.GetRelativeUri("movies/{id}{mediaTypeExtension}/users", "/movies/{id}"));
		}

		[Test]
		public void should_replace_duplicated_params()
		{
			Assert.AreEqual("users{mediaTypeExtension}", UrlGeneratorHelper.GetRelativeUri("movies/{id}/{mediaTypeExtension}/users{mediaTypeExtension}", "/movies/{id}"));
		}

		[Test]
		public void should_fix_controller_uri()
		{
			Assert.AreEqual("movies/{id}", UrlGeneratorHelper.FixControllerRoutePrefix("/movies/{id}{mediaTypeExtension}"));
		}

		[Test]
		public void should_fix_controller_uri2()
		{
			Assert.AreEqual("movies", UrlGeneratorHelper.FixControllerRoutePrefix("/movies/{mediaTypeExtension}"));
		}
	}
}