﻿namespace Sitecore.FakeDb.Tests.Data.DataProviders
{
  using System.Linq;
  using FluentAssertions;
  using Sitecore.Data;
  using Sitecore.Data.DataProviders;
  using Sitecore.FakeDb.Data.DataProviders;
  using Sitecore.FakeDb.Data.Engines;
  using Sitecore.Reflection;
  using Xunit;

  public class FakeDataProviderTest
  {
    private readonly FakeDataProvider dataProvider;

    private readonly DataStorage dataStorage;

    public FakeDataProviderTest()
    {
      var database = Database.GetDatabase("master");
      this.dataStorage = new DataStorage(database);
      this.dataStorage.FakeItems.Clear();
      this.dataStorage.FakeTemplates.Clear();

      this.dataProvider = new FakeDataProvider(this.dataStorage);
      ReflectionUtil.CallMethod(database, "AddDataProvider", new object[] { this.dataProvider });
    }

    [Fact]
    public void ShouldGetTemplateIds()
    {
      // arrange
      var t1 = this.CreateTestTemplateInDataStorage();
      var t2 = this.CreateTestTemplateInDataStorage();

      // act
      var templateIds = this.dataProvider.GetTemplateItemIds(null);

      // assert
      templateIds.Count.Should().Be(2);
      templateIds.Should().Contain(t1.ID);
      templateIds.Should().Contain(t2.ID);
    }

    [Fact]
    public void ShouldGetTemplatesFromDataStorage()
    {
      // arrange
      var t1 = this.CreateTestTemplateInDataStorage();
      var t2 = this.CreateTestTemplateInDataStorage();

      // act
      var templates = this.dataProvider.GetTemplates(null);

      // assert
      templates.Count.Should().Be(2);
      templates.GetTemplate(t1.ID).Name.Should().Be(t1.Name);
      templates.GetTemplate(t2.ID).Name.Should().Be(t2.Name);
    }

    [Fact]
    public void ShouldGetTemplatesWithDefaultDataSectionFromDataStorage()
    {
      // arrange
      this.CreateTestTemplateInDataStorage();

      var result = this.dataProvider.GetTemplates(null).First();

      // act & assert
      result.GetSection("Data").Should().NotBeNull();
    }

    [Fact]
    public void ShouldGetTemplateWithBaseTemplateField()
    {
      // arrange
      var template = this.CreateTestTemplateInDataStorage();

      // act
      var result = this.dataProvider.GetTemplates(null).First();

      // assert
      result.GetField("__Base template").Should().NotBeNull();
      result.GetField(FieldIDs.BaseTemplate).Should().NotBeNull();
    }

    [Fact]
    public void ShouldGetTemplateFields()
    {
      // arrange
      var template = this.CreateTestTemplateInDataStorage();
      template.Fields.Add("Title");

      var result = this.dataProvider.GetTemplates(null).First();

      // act & assert
      result.GetField("Title").Should().NotBeNull();
    }

    [Fact]
    public void ShouldGetTemplateFieldType()
    {
      // arrange
      var template = this.CreateTestTemplateInDataStorage();
      template.Fields.Add(new DbField("Link") { Type = "General Link" });

      var result = this.dataProvider.GetTemplates(null).First();

      // act & assert
      result.GetField("Link").Type.Should().Be("General Link");
      result.GetField("Link").TypeKey.Should().Be("general link");
    }

    [Fact]
    public void ShouldGetTemplateFieldIsShared()
    {
      // arrange
      var template = this.CreateTestTemplateInDataStorage();
      template.Fields.Add(new DbField("Title") { Shared = true });

      var result = this.dataProvider.GetTemplates(null).First();

      // act & assert
      result.GetField("Title").IsShared.Should().BeTrue();
    }

    [Fact]
    public void ShouldBeIRequireDataStorage()
    {
      // assert
      this.dataProvider.Should().BeAssignableTo<IRequireDataStorage>();
    }

    [Fact]
    public void ShouldSetDataStorage()
    {
      // arrange
      var ds = new DataStorage(Database.GetDatabase("master"));

      // act
      this.dataProvider.SetDataStorage(ds);

      // assert
      this.dataProvider.DataStorage.Should().Be(ds);
    }

    [Fact]
    private void ShouldSupportGetLanguagesAndReturnOne()
    {
      // arrange
      var db = this.dataStorage.Database;

      // act
      var langs = this.dataProvider.GetLanguages(new CallContext(db.DataManager, db.GetDataProviders().Count()));

      // assert
      langs.Should().HaveCount(1);
      langs.First().Name.Should().Be("en-US");
    }

    private DbTemplate CreateTestTemplateInDataStorage()
    {
      var templateId = ID.NewID;
      var template = new DbTemplate(templateId.ToString(), templateId);
      this.dataStorage.FakeTemplates.Add(template.ID, template);

      return template;
    }
  }
}