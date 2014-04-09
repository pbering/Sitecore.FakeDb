﻿namespace Sitecore.FakeDb.Tests
{
  using System;
  using System.Xml;
  using FluentAssertions;
  using NSubstitute;
  using Sitecore.Configuration;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Data.Managers;
  using Sitecore.Exceptions;
  using Sitecore.FakeDb.Pipelines;
  using Sitecore.FakeDb.Security.AccessControl;
  using Sitecore.Globalization;
  using Xunit;
  using Xunit.Extensions;
  using Version = Sitecore.Data.Version;

  public class DbTest
  {
    private readonly ID itemId = ID.NewID;

    [Fact]
    public void ShouldCreateCoupleOfItemsWithFields()
    {
      // act
      using (var db = new Db
                   {
                     new DbItem("item1") { { "Title", "Welcome from item 1!" } },
                     new DbItem("item2") { { "Title", "Welcome from item 2!" } }
                   })
      {
        var item1 = db.Database.GetItem("/sitecore/content/item1");
        var item2 = db.Database.GetItem("/sitecore/content/item2");

        // assert
        item1["Title"].Should().Be("Welcome from item 1!");
        item2["Title"].Should().Be("Welcome from item 2!");
      }
    }

    [Fact]
    public void ShouldCreateItemHierarchyAndReadChildByPath()
    {
      // arrange & act
      using (var db = new Db { new DbItem("parent") { new DbItem("child") } })
      {
        // assert
        db.GetItem("/sitecore/content/parent/child").Should().NotBeNull();
      }
    }

    [Fact]
    public void ShouldCreateItemInCustomLanguage()
    {
      // arrange & act
      using (var db = new Db { new DbItem("home") { Fields = { new DbField("Title") { { "da", "Hej!" } } } } })
      {
        var item = db.Database.GetItem("/sitecore/content/home", Language.Parse("da"));

        // assert
        item["Title"].Should().Be("Hej!");
        item.Language.Should().Be(Language.Parse("da"));
      }
    }

    [Fact]
    public void ShouldCreateItemInSpecificLanguage()
    {
      // arrange & act
      using (var db = new Db { new DbItem("home") { new DbField("Title") { { "en", "Hello!" }, { "da", "Hej!" } } } })
      {
        db.Database.GetItem("/sitecore/content/home", Language.Parse("en"))["Title"].Should().Be("Hello!");
        db.Database.GetItem("/sitecore/content/home", Language.Parse("da"))["Title"].Should().Be("Hej!");
      }
    }

    [Fact]
    public void ShouldCreateItemOfPredefinedTemplate()
    {
      // arrange
      var templateId = ID.NewID;

      // act
      using (var db = new Db
                   {
                     new DbTemplate("products", templateId) { "Name" },
                     new DbItem("Apple") { TemplateID = templateId }
                   })
      {
        // assert
        var item = db.Database.GetItem("/sitecore/content/apple");
        item.TemplateID.Should().Be(templateId);
        item.Fields["Name"].Should().NotBeNull();
      }
    }

    [Fact]
    public void ShouldCreateItemOfVersionOne()
    {
      // arrange & act
      using (var db = new Db { new DbItem("home") })
      {
        var item = db.Database.GetItem("/sitecore/content/home");

        // assert
        item.Version.Should().Be(Version.First);
        item.Versions.Count.Should().Be(1);
        item.Versions[Version.First].Should().NotBeNull();
      }
    }

    [Fact]
    public void ShouldCreateItemTemplate()
    {
      // arrange & act
      using (var db = new Db { new DbTemplate("products") })
      {
        // assert
        db.Database.GetTemplate("products").Should().NotBeNull();
      }
    }

    [Fact]
    public void ShouldCreateItemWithFields()
    {
      // act
      using (var db = new Db { new DbItem("home", this.itemId) { { "Title", "Welcome!" } } })
      {
        var item = db.Database.GetItem(this.itemId);

        // assert
        item["Title"].Should().Be("Welcome!");
      }
    }

    [Fact]
    public void ShouldCreateItemWithFieldsAndChildren()
    {
      // arrange & act
      using (var db = new Db
                   {
                     new DbItem("parent")
                       {
                         Fields = new DbFieldCollection { { "Title", "Welcome to parent item!" } },
                         Children = new[] { new DbItem("child") { { "Title", "Welcome to child item!" } } }
                       }
                   })
      {
        // assert
        var parent = db.GetItem("/sitecore/content/parent");
        parent["Title"].Should().Be("Welcome to parent item!");
        parent.Children["child"]["Title"].Should().Be("Welcome to child item!");
      }
    }

    [Fact]
    public void ShouldCreateItemWithUnversionedSharedFieldsByDefault()
    {
      // arrange & act
      using (var db = new Db { new DbItem("home") { { "Title", "Hello!" } } })
      {
        db.Database.GetItem("/sitecore/content/home", Language.Parse("en"))["Title"].Should().Be("Hello!");
        db.Database.GetItem("/sitecore/content/home", Language.Parse("uk-UA"))["Title"].Should().Be("Hello!");
      }
    }

    [Fact]
    public void ShouldCreateSimpleItem()
    {
      // arrange
      var id = new ID("{91494A40-B2AE-42B5-9469-1C7B023B886B}");

      // act
      using (var db = new Db { new DbItem("myitem", id) })
      {
        var i = db.Database.GetItem(id);

        // assert
        i.Should().NotBeNull();
        i.Name.Should().Be("myitem");
      }
    }

    [Fact]
    public void ShouldDenyItemCreateAccess()
    {
      // arrange
      using (var db = new Db { new DbItem("home") { Access = new DbItemAccess { CanCreate = false } } })
      {
        var item = db.GetItem("/sitecore/content/home");

        // act
        Action action = () => item.Add("child", item.Template);

        // assert
        action.ShouldThrow<AccessDeniedException>();
      }
    }

    [Fact]
    public void ShouldDenyItemReadAccess()
    {
      // arrange & act
      using (var db = new Db { new DbItem("home") { Access = new DbItemAccess { CanRead = false } } })
      {
        // assert
        db.GetItem("/sitecore/content/home").Should().BeNull();
      }
    }

    [Fact]
    public void ShouldDenyItemWriteAccess()
    {
      // arrange
      using (var db = new Db { new DbItem("home") { Access = new DbItemAccess { CanWrite = false } } })
      {
        var item = db.GetItem("/sitecore/content/home");

        // act
        Action action = () => new EditContext(item);

        // assert
        action.ShouldThrow<UnauthorizedAccessException>();
      }
    }

    [Fact]
    public void ShouldEditItem()
    {
      // arrange
      using (var db = new Db { new DbItem("home") { { "Title", "Hello!" } } })
      {
        var item = db.Database.GetItem("/sitecore/content/home");

        // act
        using (new EditContext(item))
        {
          item["Title"] = "Welcome!";
        }

        // assert
        item["Title"].Should().Be("Welcome!");
      }
    }

    [Fact]
    public void ShouldGenerateTemplateIdIfNotSet()
    {
      // arrange
      var template = new DbTemplate { ID = null };

      // act
      using (new Db { template })
      {
        // assert
        template.ID.Should().NotBeNull();
        template.ID.Should().NotBe(ID.Null);
      }
    }

    [Fact]
    public void ShouldGetItemById()
    {
      // arrange
      var id = ID.NewID;
      using (var db = new Db { new DbItem("my item", id) })
      {
        // act & assert
        db.GetItem(id).Should().NotBeNull();
      }
    }

    [Fact]
    public void ShouldGetItemByIdAndLanguage()
    {
      // arrange
      var id = ID.NewID;
      using (var db = new Db { new DbItem("my item", id) })
      {
        // act & assert
        db.GetItem(id, "en").Should().NotBeNull();
      }
    }

    [Fact]
    public void ShouldGetItemByIdLanguageAndVersion()
    {
      // arrange
      var id = ID.NewID;
      using (var db = new Db { new DbItem("my item", id) })
      {
        // act & assert
        db.GetItem(id, "en", 1).Should().NotBeNull();
      }
    }

    [Fact]
    public void ShouldGetItemByPath()
    {
      // arrange
      using (var db = new Db { new DbItem("my item") })
      {
        // act & assert
        db.GetItem("/sitecore/content/my item").Should().NotBeNull();
      }
    }

    [Fact]
    public void ShouldGetItemByPathAndLanguage()
    {
      // arrange
      using (var db = new Db { new DbItem("home") })
      {
        // act & assert
        db.GetItem("/sitecore/content/home", "en").Should().NotBeNull();
      }
    }

    [Fact]
    public void ShouldGetItemFromSitecoreDatabase()
    {
      // arrange
      using (var db = new Db())
      {
        // act & assert
        db.GetItem("/sitecore/content").Should().NotBeNull();
      }
    }

    [Fact]
    public void ShouldGetItemParent()
    {
      // arrange
      using (var db = new Db { new DbItem("item") })
      {
        // act
        var parent = db.GetItem("/sitecore/content/item").Parent;

        // assert
        parent.Paths.FullPath.Should().Be("/sitecore/content");
      }
    }

    [Fact]
    public void ShouldHaveDefaultMasterDatabase()
    {
      // arrange
      var db = new Db();

      // act & assert
      db.Database.Name.Should().Be("master");
    }

    [Fact]
    public void ShouldInitializeDataStorage()
    {
      // arrange & act
      using (var db = new Db())
      {
        // assert
        db.DataStorage.Should().NotBeNull();
      }
    }

    [Fact]
    public void ShouldNotFailWhenAddingVersion()
    {
      // arrange
      using (var db = new Db { new DbItem("home") })
      {
        var item = db.Database.GetItem("/sitecore/content/home");

        // act
        var result = item.Versions.AddVersion();

        // assert
        result.Versions.Count.Should().Be(1);
      }
    }

    [Fact]
    public void ShouldNotShareTemplateForItemsIfTemplatesSetExplicitly()
    {
      // arrange & act
      using (var db = new Db
                   {
                     new DbItem("article 1") { { "Title", "A1" } },
                     new DbItem("article 2", ID.NewID, ID.NewID) { { "Title", "A2" } }
                   })
      {
        var template1 = db.GetItem("/sitecore/content/article 1").TemplateID;
        var template2 = db.GetItem("/sitecore/content/article 2").TemplateID;

        // assert
        template1.Should().NotBe(template2);
      }
    }

    [Fact]
    public void ShouldNotShareTemplateForItemsWithDifferentFields()
    {
      // arrange & act
      using (var db = new Db
                   {
                     new DbItem("some item") { { "some field", "some value" } },
                     new DbItem("another item") { { "another field", "another value" } }
                   })
      {
        var template1 = db.GetItem("/sitecore/content/some item").TemplateID;
        var template2 = db.GetItem("/sitecore/content/another item").TemplateID;

        // assert
        template1.Should().NotBe(template2);
      }
    }

    [Fact]
    public void ShouldReadDefaultContentItem()
    {
      // arrange
      using (var db = new Db())
      {
        // act
        var item = db.Database.GetItem(ItemIDs.ContentRoot);

        // assert
        item.Should().NotBeNull();
      }
    }

    [Fact]
    public void ShouldReadFieldValueByIdAndName()
    {
      // arrange
      var fieldId = ID.NewID;
      using (var db = new Db
                        {
                          new DbItem("home") { new DbField("Title") { ID = fieldId, Value = "Hello!" } }
                        })
      {
        // act
        var item = db.GetItem("/sitecore/content/home");

        // assert
        item[fieldId].Should().Be("Hello!");
        item["Title"].Should().Be("Hello!");
      }
    }

    [Fact]
    public void ShouldRenameItem()
    {
      // arrange
      using (var db = new Db { new DbItem("home") })
      {
        var home = db.Database.GetItem("/sitecore/content/home");

        // act
        using (new EditContext(home))
        {
          home.Name = "new home";
        }

        // assert
        db.Database.GetItem("/sitecore/content/new home").Should().NotBeNull();
        db.Database.GetItem("/sitecore/content/new home").Name.Should().Be("new home");
        db.Database.GetItem("/sitecore/content/home").Should().BeNull();
      }
    }

    [Theory]
    [InlineData("master")]
    [InlineData("web")]
    [InlineData("core")]
    public void ShouldResolveDatabaseByName(string name)
    {
      // arrange
      var db = new Db(name);

      // act & assert
      db.Database.Name.Should().Be(name);
    }

    [Fact]
    public void ShouldSetAndGetCustomSettings()
    {
      // arrange
      using (var db = new Db())
      {
        // act
        db.Configuration.Settings["my setting"] = "my new value";

        // assert
        Settings.GetSetting("my setting").Should().Be("my new value");
      }
    }

    [Fact]
    public void ShouldCleanUpSettingsAfterDispose()
    {
      // arrange
      using (var db = new Db())
      {
        // act
        db.Configuration.Settings["my setting"] = "my new value";
      }

      // assert
      Settings.GetSetting("my setting").Should().BeEmpty();
    }

    [Fact]
    public void ShouldSetChildItemFullIfParentIdIsSet()
    {
      // arrange
      var parent = new DbItem("parent");
      var child = new DbItem("child");

      // act
      using (var db = new Db { parent })
      {
        child.ParentID = parent.ID;
        db.Add(child);

        // assert
        child.FullPath.Should().Be("/sitecore/content/parent/child");
      }
    }

    [Fact]
    public void ShouldSetChildItemFullPathOnDbInit()
    {
      // arrange
      var parent = new DbItem("parent");
      var child = new DbItem("child");

      parent.Add(child);

      // act
      using (new Db { parent })
      {
        // assert
        child.FullPath.Should().Be("/sitecore/content/parent/child");
      }
    }

    [Fact]
    public void ShouldSetDatabaseInDataStorage()
    {
      // arrange & act
      using (var db = new Db())
      {
        // assert
        db.DataStorage.Database.Should().BeSameAs(db.Database);
      }
    }

    [Fact]
    public void ShouldSetDefaultLanguage()
    {
      // arrange & act
      using (var db = new Db { new DbItem("home") })
      {
        var item = db.Database.GetItem("/sitecore/content/home");

        // assert
        item.Language.Should().Be(Language.Parse("en"));
      }
    }

    [Fact]
    public void ShouldSetSitecoreContentFullPathByDefault()
    {
      // arrange
      var item = new DbItem("home");

      // act
      using (new Db { item })
      {
        // asert
        item.FullPath.Should().Be("/sitecore/content/home");
      }
    }

    [Fact]
    public void ShouldSetSitecoreContentParentIdByDefault()
    {
      // arrange
      var item = new DbItem("home");

      // act
      using (new Db { item })
      {
        // assert
        item.ParentID.Should().Be(ItemIDs.ContentRoot);
      }
    }

    [Fact]
    public void ShouldShareTemplateForItemsWithFields()
    {
      // arrange & act
      using (var db = new Db
                        {
                          new DbItem("article 1") { { "Title", "A1" } },
                          new DbItem("article 2") { { "Title", "A2" } }
                        })
      {
        var template1 = db.GetItem("/sitecore/content/article 1").TemplateID;
        var template2 = db.GetItem("/sitecore/content/article 2").TemplateID;

        // assert
        template1.Should().Be(template2);
      }
    }

    [Fact]
    public void ShouldThrowExceptionIfNoDbInstanceInitialized()
    {
      // arrange
      Factory.Reset();

      // act
      Action action = () => Database.GetDatabase("master").GetItem("/sitecore/content");

      // assert
      action.ShouldThrow<InvalidOperationException>().WithMessage("Sitecore.FakeDb.Db instance has not been initialized.");
    }

    [Fact]
    public void ShouldThrowExceptionIfTemplateIdIsAlreadyExists()
    {
      // arrange
      var id = ID.NewID;
      using (var db = new Db { new DbTemplate("products", id) })
      {
        // act
        Action action = () => db.Add(new DbTemplate("products", id));

        // assert
        action.ShouldThrow<ArgumentException>().WithMessage("A tamplete with the same id has already been added.*");
      }
    }

    [Fact]
    public void ShouldInitializeDbConfigurationUsingFactoryConfiguration()
    {
      // arrange
      using (var db = new Db())
      {
        // act & assert
        db.Configuration.Settings.ConfigSection.Should().BeEquivalentTo(Factory.GetConfiguration());
      }
    }

    [Fact]
    public void ShouldInitializePipelineWatcherUsingFactoryConfiguration()
    {
      // arrange
      using (var db = new Db())
      {
        // act & assert
        db.PipelineWatcher.ConfigSection.Should().BeEquivalentTo(Factory.GetConfiguration());
      }
    }

    [Fact]
    public void ShoudDisposePipelineWatcher()
    {
      // arrange
      var watcher = Substitute.For<PipelineWatcher, IDisposable>(new XmlDocument());
      var db = new Db(watcher);

      // act
      db.Dispose();

      // assert
      watcher.Received().Dispose();
    }

    [Fact]
    public void ShouldBeEqualsButNotSame()
    {
      // arrange
      using (var db = new Db { new DbItem("home") })
      {
        // act
        var item1 = db.GetItem("/sitecore/content/Home");
        var item2 = db.GetItem("/sitecore/content/Home");

        // assert
        item1.Should().Be(item2);
        item1.Should().NotBeSameAs(item2);
      }
    }

    [Fact]
    public void ShouldCreateVersionedItem()
    {
      using (var db = new Db
                   {
                     new DbItem("home")
                       {
                         Fields =
                           {
                             new DbField("Title")
                               {
                                 { "en", 1, "title version 1" },
                                 { "en", 2, "title version 2" }
                               }
                           }
                       }
                   })
      {
        var item1 = db.Database.GetItem("/sitecore/content/home", Language.Parse("en"), Version.Parse(1));
        item1["Title"].Should().Be("title version 1");
        item1.Version.Number.Should().Be(1);

        var item2 = db.Database.GetItem("/sitecore/content/home", Language.Parse("en"), Version.Parse(2));
        item2["Title"].Should().Be("title version 2");
        item2.Version.Number.Should().Be(2);
      }
    }

    [Fact]
    public void ShouldGetItemVersionsCount()
    {
      // arrange
      using (var db = new Db
                   {
                     new DbItem("home")
                       {
                         Fields =
                           {
                             new DbField("Title") { { "en", 1, "v1" }, { "en", 2, "v2" } }
                           }
                       }
                   })
      {
        var item = db.GetItem("/sitecore/content/home");

        // act & assert
        item.Versions.Count.Should().Be(2);
      }
    }

    [Fact]
    public void ShouldCreateItemVersion()
    {
      // arrange
      using (var db = new Db { new DbItem("home") { { "Title", "hello" } } })
      {
        var item1 = db.GetItem("/sitecore/content/home");

        // act
        var item2 = item1.Versions.AddVersion();
        using (new EditContext(item2))
        {
          item2["Title"] = "Hi there!";
        }

        // assert
        item1["Title"].Should().Be("hello");
        item2["Title"].Should().Be("Hi there!");

        db.GetItem("/sitecore/content/home", "en", 1)["Title"].Should().Be("hello");
        db.GetItem("/sitecore/content/home", "en", 2)["Title"].Should().Be("Hi there!");
      }
    }

    [Fact]
    public void ShouldCreateAndFulfilCompositeFieldsStructure()
    {
      // arrange
      using (var db = new Db())
      {
        // act
        db.Add(new DbItem("item1") { { "field1", "item1-field1-value" }, { "field2", "item1-field2-value" } });
        db.Add(new DbItem("item2") { { "field1", "item2-field1-value" }, { "field2", "item2-field2-value" } });

        // assert
        db.GetItem("/sitecore/content/item1")["field1"].Should().Be("item1-field1-value");
        db.GetItem("/sitecore/content/item1")["field2"].Should().Be("item1-field2-value");
        db.GetItem("/sitecore/content/item2")["field1"].Should().Be("item2-field1-value");
        db.GetItem("/sitecore/content/item2")["field2"].Should().Be("item2-field2-value");
      }
    }

    [Theory]
    [InlineData("$name", "Home")]
    [InlineData("static-text", "static-text")]
    public void ShouldCreateTemplateWithStandardValues(string standardValue, string expectedValue)
    {
      // arrange
      var templateId = ID.NewID;

      using (var db = new Db { new DbTemplate("sample", templateId) { { "Title", standardValue } } })
      {
        var root = db.GetItem(ItemIDs.ContentRoot);

        // act
        var item = ItemManager.CreateItem("Home", root, templateId);

        // assert
        item["Title"].Should().Be(expectedValue);
      }
    }

    [Fact]
    public void ShouldCreateItemOfFolderTemplate()
    {
      // arrange & act
      using (var db = new Db { new DbItem("Sample") { TemplateID = TemplateIDs.Folder } })
      {
        // assert
        db.GetItem("/sitecore/content/sample").TemplateID.Should().Be(TemplateIDs.Folder);
      }
    }

    [Fact]
    public void ShouldCreateSampleTemplateIfTemplateIdIsSetButTemplateIsMissing()
    {
      // arrange
      var templateId = ID.NewID;

      // act
      using (var db = new Db { new DbItem("home", ID.NewID, templateId) })
      {
        // assert
        db.GetItem("/sitecore/content/home").TemplateID.Should().Be(templateId);
      }
    }
  }
}