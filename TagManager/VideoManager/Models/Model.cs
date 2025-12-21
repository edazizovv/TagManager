using CsvHelper;
using Dapper;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Npgsql;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace VideoManager.Models
{

    public enum ExplorerDefaultAction
    {
        None = 0,
        CopyValue = 1,
        OpenLink = 2
    }

    public class AntiforgeryTokenDto
    {
        public string Token { get; set; } = string.Empty;
    }

    public class Realm
    {
        [Required(ErrorMessage = "Name required")]
        [RegularExpression("^[a-z_]{2,3}$", ErrorMessage = "String must contain only a-z literals or underscore and be 2-3 characters length")]
        public string name { get; set; }
        [Required(ErrorMessage = "View required")]
        [RegularExpression("^[a-zA-Z_:\\\\]{3,100}$", ErrorMessage = "String must contain only a-z literals or underscore or slashes and be 3-100 characters length")]
        public string view { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Default action is required.")]
        public ExplorerDefaultAction DefaultAction { get; set; } = ExplorerDefaultAction.None;
        public string? PreferredApp { get; set; }
    }

    public class EmptyPizza
    {
        public string realm { get; set; }
        public int id { get; set; }
        public string _name { get; set; }
        public string _author { get; set; }
        public string thumbnail { get; set; }
        public string _link { get; set; }
    }

    public class PizzaTag
    {
        public int pizzaId { get; set; }
        public string tagName { get; set; }
    }

    public class Pizza
    {
        public string realm { get; set; }
        public ExplorerDefaultAction DefaultAction { get; set; }
        public string PreferredApp { get; set; }
        public string StoragePath { get; set; }
        public int id { get; set; }
        public string _name { get; set; }
        public string _author { get; set; }
        public string thumbnail { get; set; }
        public string _link { get; set; }

        public List<string> Tags { get; set; }
    }

    public class PizzaRow
    {
        public string realm { get; set; }
        public ExplorerDefaultAction DefaultAction { get; set; }
        public string PreferredApp { get; set; }
        public string StoragePath { get; set; }
        public int id { get; set; }
        public string _name { get; set; }
        public string _author { get; set; }
        public string thumbnail { get; set; }
        public string _link { get; set; }

        public string? Tag { get; set; }
    }

    public class ShortTag
    {
        [Required(ErrorMessage = "Tags required")]
        // TBD!
        // [RegularExpression("^[a-z_]{3-10}$", ErrorMessage = "String must contain only a-z literals or underscore and be 3-10 characters length")]
        [RegularExpression("^[a-z_]{3,10}$", ErrorMessage = "String must contain only a-z literals or underscore and be 3-10 characters length")]
        public string? Value { get; set; }

        public ShortTag(string value)
        {
            this.Value = value;
        }

        public ShortTag()
        {
        }
    }

    public class Tag
    {
        [Range(0, int.MaxValue, ErrorMessage = "Pizza must be selected")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tags required")]
        //[RegularExpression("^[a-z_]{3-10}$", ErrorMessage = "String must contain only a-z literals or underscore and be 3-10 characters length")]
        public string? Value { get; set; }

        public Tag()
        {
            this.Id = -1;
        }
    }

    public class SearchTag
    {
        [Required(ErrorMessage = "Tags required")]
        //[RegularExpression("^[a-z_]{3-10}$", ErrorMessage = "String must contain only a-z literals or underscore and be 3-10 characters length")]
        public string? Value { get; set; }
    }

    public interface IDbService
    {
        public Task<T> GetAsync<T>(string command, object parameters);
        public Task<List<T>> GetAll<T>(string command, object parameters);
        public Task<T> Insert<T>(string command, object parameters);
        public Task<T> Update<T>(string command, object parameters);
        public Task<T> Delete<T>(string command, object parameters);
    }

    public class DbService : IDbService
    {
        private readonly IDbConnection _db;

        public DbService(IConfiguration config)
        {
            _db = new NpgsqlConnection(config.GetConnectionString("PizzaDB"));
        }

        public async Task<T> GetAsync<T>(string command, object parameters)
        {
            T result;
            result = (await _db.QueryAsync<T>(command, parameters).ConfigureAwait(false)).FirstOrDefault();
            return result;
        }

        public async Task<List<T>> GetAll<T>(string command, object parameters)
        {
            List<T> result = new List<T>();
            result = (await _db.QueryAsync<T>(command, parameters)).ToList();
            return result;
        }

        public async Task<T> Insert<T>(string command, object parameters)
        {
            T result;
            result = _db.Query<T>(command, parameters).FirstOrDefault();
            return result;
        }

        public async Task<T> Update<T>(string command, object parameters)
        {
            T result;
            result = _db.Query<T>(command, parameters).FirstOrDefault();
            return result;
        }

        public async Task<T> Delete<T>(string command, object parameters)
        {
            T result;
            result = _db.Query<T>(command, parameters).FirstOrDefault();
            return result;
        }
    }

    public interface IPizzaService
    {
        public Task<bool> CreatePizza(EmptyPizza pizza);
        public Task<List<Pizza>> GetPizzaList(string selectedRealm, List<string> filterTags);
        public Task<Pizza> UpdatePizza(Pizza pizza);
        // public Task<bool> DeletePizza(int key);
    }

    public class PizzaService : IPizzaService
    {
        private readonly IDbService _dbService;

        public PizzaService(IDbService dbService)
        {
            _dbService = dbService;
        }

        
        public async Task<bool> CreatePizza(EmptyPizza pizza)
        {
            string insertQuery = @"
                INSERT INTO public.pizzas (realm, id, _name, _author, thumbnail, _link) 
                VALUES (@realm, @id, @_name, @_author, @thumbnail, @_link)
            ";
            var result = await _dbService.Insert<int>(insertQuery, pizza);
            
            return true;
        }
        

        public async Task<List<Pizza>> GetPizzaList(string selectedRealm, List<string> filterTags)
        {

            string baseQuery = @"
                SELECT p.realm
                     , r.default_action as DefaultAction
                     , r.preferred_app as PreferredApp
                     , r.view as StoragePath
                     , p.id
                     , p._name
                     , p._author
                     , '\base\' || r.name || '\thmb\' || p.thumbnail AS thumbnail
                     , p._link
                     , coalesce(t.tag, 'nope') as tag
                FROM 
                public.pizzas AS p
                LEFT JOIN 
                public.accessories AS r 
                ON 
                p.realm = r.name
                LEFT JOIN 
                public.pizzatag AS t 
                ON 
                p.id = t.id
                WHERE p.realm = @realm
            ";

            
            IEnumerable<PizzaRow> pizzaRows;

            if (filterTags != null && filterTags.Count > 0)
            {

                var tagSet = "(" + string.Join(", ", filterTags.Select(t => $"'{t}'")) + ")";
                int tagCount = filterTags.Count;

                string filteredQuery = baseQuery + $@"
                    AND p.id IN (
                        SELECT x.id
                        FROM 
                        (
                        SELECT z.id
                             , z.tag
                        FROM public.pizzatag AS z
                        UNION 
                        (
                        SELECT DISTINCT p.id
                                      , 'nope' as tag
                        FROM 
                        public.pizzas AS p
                        LEFT JOIN
                        public.pizzatag AS t 
                        ON 
                        p.id = t.id
                        WHERE t.tag IS NULL
                        )
                        ) AS x
                        WHERE x.tag IN {tagSet}
                        GROUP BY x.id
                        HAVING COUNT(DISTINCT x.tag) = {tagCount}
                    )
                ";

                pizzaRows = await _dbService.GetAll<PizzaRow>(filteredQuery, new { realm = selectedRealm });

            }
            else
            {
                pizzaRows = await _dbService.GetAll<PizzaRow>(baseQuery, new { realm = selectedRealm });
            }

            List<Pizza> pizzaList = pizzaRows
                .GroupBy(r => new { r.realm, r.DefaultAction, r.PreferredApp, r.StoragePath, r.id, r._name, r._author, r._link, r.thumbnail })
                .Select(g => new Pizza
                {
                    realm = g.Key.realm,
                    DefaultAction = g.Key.DefaultAction,
                    PreferredApp = g.Key.PreferredApp,
                    StoragePath = g.Key.StoragePath,
                    id = g.Key.id,
                    _name = g.Key._name,
                    _author = g.Key._author,
                    _link = g.Key._link,
                    thumbnail = g.Key.thumbnail,
                    Tags = g.Where(r => r.Tag != null)
                            .Select(r => r.Tag!)
                            .Distinct()
                            .ToList()
                })
                .ToList();

            return pizzaList;
        }

        public async Task<Pizza> UpdatePizza(Pizza pizza)
        {
            // TODO: no updates to the pizzas' main fields should be applied, only some specific fields, TBD
            // not used ATM, let it be
            var updatePizza = await _dbService.Update<int>("UPDATE public.pizzas SET name=@Name WHERE id=@Id", pizza);
            return pizza;
        }

        /*
        public async Task<bool> DeletePizza(int key)
        {
            var deletePizza = await _dbService.Delete<int>("DELETE FROM public.pizzas WHERE id=@Id", new { Id = key });
            return true;
        }
        */
    }

    public interface ITagService
    {
        public Task<bool> CreateTag(ShortTag tag);
        public Task<List<string>> GetTagList(string selectedRealm);
        public Task<bool> DeleteTag(ShortTag tag);
    }

    public class TagService : ITagService
    {
        private readonly IDbService _dbService;

        public TagService(IDbService dbService)
        {
            _dbService = dbService;
        }

        public async Task<bool> CreateTag(ShortTag tag)
        {
            var result = await _dbService.Insert<int>("INSERT INTO public.tags (tag) VALUES (@Tag) ON CONFLICT (tag) DO NOTHING", new { Tag = tag.Value });
            return true;
        }

        public async Task<List<string>> GetTagList(string selectedRealm)
        {
            // TODO: implement realm-specific tags
            string tagQuery = @"
                SELECT tag
                FROM public.tags
            ";
            var tagList = await _dbService.GetAll<string>(tagQuery, new { });
            tagList.Add("nope");
            return tagList;
        }

        public async Task<bool> DeleteTag(ShortTag tag)
        {
            var deleteTag = await _dbService.Delete<int>("DELETE FROM public.tags WHERE tag=@Tag", new { Tag = tag.Value });
            return true;
        }

    }

    public interface IRealmService
    {
        public Task<bool> AddOrUpdateRealm(Realm realm);
        public Task<List<Realm>> GetRealmList();
        public Task<bool> DeleteRealm(string realmName);
    }

    public class RealmService : IRealmService
    {
        private readonly IDbService _dbService;

        public RealmService(IDbService dbService)
        {
            _dbService = dbService;
        }

        public async Task<bool> AddOrUpdateRealm(Realm realm)
        {
            string baseQuery = @"
                SELECT *
                FROM public.accessories
                WHERE name = @name
                ;
            ";
            var realmList = await _dbService.GetAll<Realm>(baseQuery, new { name = realm.name });
            if (realmList.Count() == 1)
            {
                var updateQuery = @"
                    UPDATE public.accessories
                    SET view = @RealmView, default_action = @DefaultAction, preferred_app = @PreferredApp
                    WHERE name = @RealmName
                    ;
                ";
                var result = await _dbService.Update<int>(
                    updateQuery,
                    new { RealmName = realm.name, RealmView = realm.view, DefaultAction = realm.DefaultAction, PreferredApp = realm.PreferredApp }
                    );
            }
            else
            {
                var result = await _dbService.Insert<int>(
                    "INSERT INTO public.accessories (name, view, default_action, preferred_app) " +
                    "VALUES (@RealmName, @RealmView, @DefaultAction, @PreferredApp)",
                    new { RealmName = realm.name, RealmView = realm.view, DefaultAction = realm.DefaultAction, PreferredApp = realm.PreferredApp }
                    );
            }
            return true;
        }

        public async Task<List<Realm>> GetRealmList()
        {
            var realmList = await _dbService.GetAll<Realm>("SELECT * FROM public.accessories", new { });
            return realmList;
        }

        public async Task<bool> DeleteRealm(string realmName)
        {
            var deleteRealm = await _dbService.Delete<int>(
                "DELETE FROM public.accessories " +
                "WHERE name=@RealmName", new { RealmName = realmName });
            return true;
        }

    }

    public interface IPizzaTagService
    {
        public Task<bool> CreateTag(PizzaTag pt);
        public Task<List<string>> GetTags(int pizzaId);
        public Task<bool> UpdateTag(int pizzaId, string oldTag, string newTag);
        public Task<bool> DeleteTag(int pizzaId, string tag);
    }

    public class PizzaTagService : IPizzaTagService
    {
        private readonly IDbService _dbService;

        public PizzaTagService(IDbService dbService)
        {
            _dbService = dbService;
        }

        public async Task<bool> CreateTag(PizzaTag pt)
        {
            string insertQuery = @"
                INSERT INTO public.pizzatag (id, tag) 
                VALUES (@pizzaId, @tagName)
            ";
            var result = await _dbService.Insert<int>(insertQuery, pt);
            return true;
        }

        public async Task<List<string>> GetTags(int pizzaId)
        {
            var tagList = await _dbService.GetAll<string>("SELECT tag FROM public.pizzatag WHERE id=@pizzaId", new { pizzaId = pizzaId });
            return tagList;
        }

        public async Task<bool> UpdateTag(int pizzaId, string oldTag, string newTag)
        {
            var updatePizza = await _dbService.Update<int>("UPDATE public.pizzatag SET tag=@newTag WHERE id=@pizzaId AND tag=@oldTag", new { pizzaId = pizzaId, oldTag = oldTag });
            return true;
        }

        public async Task<bool> DeleteTag(int pizzaId, string tag)
        {
            var deleteTag = await _dbService.Delete<int>("DELETE FROM public.pizzatag WHERE id=@pizzaId AND tag=@tag", new { pizzaId = pizzaId, tag = tag });
            return true;
        }
    }

    public interface IExportSingleRealmService
    {
        Task<byte[]> BuildSingleZipAsync(string realmName, string realmView);
    }

    public class ExportSingleService : IExportSingleRealmService
    {
        private readonly IDbService _dbService;

        public ExportSingleService(IDbService dbService)
        {
            _dbService = dbService;
        }

        public async Task<byte[]> BuildSingleZipAsync(string realmName, string realmView)
        {
            string tagsQuery = @"
                SELECT DISTINCT t.tag AS Value
                FROM 
                public.tags as t
                LEFT JOIN
                public.pizzatag as pt
                ON t.tag = pt.tag
                LEFT JOIN
                public.pizzas as p
                ON pt.id = p.id
                WHERE p.realm = @realmName
            ";
            var tagsRows = await _dbService.GetAll<ShortTag>(
                tagsQuery, new { realmName = realmName });

            string realmsQuery = @"
                SELECT name
                     , view
                     , default_action as DefaultAction
                     , preferred_app as PreferredApp
                FROM public.accessories
                WHERE name = @realmName
            ";
            var realmsRows = await _dbService.GetAll<Realm>(
                realmsQuery, new { realmName = realmName });

            string pizzasQuery = @"
                SELECT realm
                     , id
                     , _name
                     , _author
                     , thumbnail
                     , _link
                FROM public.pizzas
                WHERE realm = @realmName
            ";
            var pizzasRows = await _dbService.GetAll<EmptyPizza>(
                pizzasQuery, new { realmName = realmName });

            string pizzaTagsQuery = @"
                SELECT DISTINCT pt.id as pizzaId
                              , pt.tag as tagName
                FROM 
                public.pizzatag AS pt
                LEFT JOIN
                public.pizzas AS p
                ON pt.id = p.id
                WHERE p.realm = @realmName
            ";
            var pizzaTagsRows = await _dbService.GetAll<PizzaTag>(
                pizzaTagsQuery, new { realmName = realmName });

            return await BuildZipSingleInternal(
                tagsRows,
                realmsRows,
                pizzasRows,
                pizzaTagsRows,
                realmName,
                realmView
            );
        }

        private async Task<byte[]> BuildZipSingleInternal(
            IEnumerable<ShortTag> tagsRows,
            IEnumerable<Realm> realmsRows,
            IEnumerable<EmptyPizza> pizzasRows,
            IEnumerable<PizzaTag> pizzaTagsRows,
            string realmName,
            string sourceFolderPath)
        {
            using var zipStream = new MemoryStream();

            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {

                var tagsCsvEntry = zip.CreateEntry("tags.csv");
                using (var entryStream = tagsCsvEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(tagsRows);
                }

                var realmsCsvEntry = zip.CreateEntry("realms.csv");
                using (var entryStream = realmsCsvEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(realmsRows);
                }

                var tabCsvEntry = zip.CreateEntry(realmName + "/tab.csv");
                using (var entryStream = tabCsvEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(pizzasRows);
                }

                var tagCsvEntry = zip.CreateEntry(realmName + "/tag.csv");
                using (var entryStream = tagCsvEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(pizzaTagsRows);
                }

                AddFolderToZip(zip, sourceFolderPath, realmName + "/");
            }

            return zipStream.ToArray();
        }

        private void AddFolderToZip(
            ZipArchive zip,
            string folderPath,
            string zipPath)
        {
            foreach (var file in Directory.GetFiles(folderPath))
            {
                var entryName = Path.Combine(zipPath, Path.GetFileName(file))
                                    .Replace("\\", "/");

                zip.CreateEntryFromFile(file, entryName);
            }

            foreach (var dir in Directory.GetDirectories(folderPath))
            {
                var dirName = Path.GetFileName(dir);
                AddFolderToZip(zip, dir, Path.Combine(zipPath, dirName));
            }
        }
    }


    public interface IExportAllRealmService
    {
        Task<byte[]> BuildAllZipAsync();
    }

    public class ExportAllService : IExportAllRealmService
    {
        private readonly IDbService _dbService;

        public ExportAllService(IDbService dbService)
        {
            _dbService = dbService;
        }

        public async Task<byte[]> BuildAllZipAsync()
        {
            string tagsQuery = @"
                SELECT tag AS Value
                FROM 
                public.tags
            ";
            var tagsRows = await _dbService.GetAll<ShortTag>(
                tagsQuery, new { });

            string realmsQuery = @"
                SELECT name
                     , view
                     , default_action as DefaultAction
                     , preferred_app as PreferredApp
                FROM public.accessories
            ";
            var realmsRows = await _dbService.GetAll<Realm>(
                realmsQuery, new { });

            string pizzasQuery = @"
                SELECT realm
                     , id
                     , _name
                     , _author
                     , thumbnail
                     , _link
                FROM public.pizzas
            ";
            var pizzasRows = await _dbService.GetAll<EmptyPizza>(
                pizzasQuery, new { });

            string pizzaTagsQuery = @"
                SELECT id as pizzaId
                     , tag as tagName
                FROM 
                public.pizzatag
            ";
            var pizzaTagsRows = await _dbService.GetAll<PizzaTag>(
                pizzaTagsQuery, new { });

            return await BuildZipInternal(
                tagsRows,
                realmsRows,
                pizzasRows,
                pizzaTagsRows
            );
        }

        private async Task<byte[]> BuildZipInternal(
            IEnumerable<ShortTag> tagsRows,
            IEnumerable<Realm> realmsRows,
            IEnumerable<EmptyPizza> pizzasRows,
            IEnumerable<PizzaTag> pizzaTagsRows)
        {

            using var zipStream = new MemoryStream();

            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {

                var tagsCsvEntry = zip.CreateEntry("tags.csv");
                using (var entryStream = tagsCsvEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(tagsRows);
                }

                var realmsCsvEntry = zip.CreateEntry("realms.csv");
                using (var entryStream = realmsCsvEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(realmsRows);
                }

                List<string> realmNames = realmsRows?
                    .Select(r => r.name)
                    .ToList()
                    ?? new List<string>();

                foreach (string realmName in realmNames)
                {

                    string? realmView = realmsRows?
                        .Where(r => r.name == realmName)
                        .Select(r => r.view)
                        .FirstOrDefault();

                    var tabCsvEntry = zip.CreateEntry(realmName + "/tab.csv");
                    using (var entryStream = tabCsvEntry.Open())
                    using (var writer = new StreamWriter(entryStream))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(pizzasRows);
                    }

                    var tagCsvEntry = zip.CreateEntry(realmName + "/tag.csv");
                    using (var entryStream = tagCsvEntry.Open())
                    using (var writer = new StreamWriter(entryStream))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(pizzaTagsRows);
                    }

                    AddFolderToZip(zip, realmView, realmName + "/");

                }

            }

            return zipStream.ToArray();
        }

        private void AddFolderToZip(
            ZipArchive zip,
            string folderPath,
            string zipPath)
        {
            foreach (var file in Directory.GetFiles(folderPath))
            {
                var entryName = Path.Combine(zipPath, Path.GetFileName(file))
                                    .Replace("\\", "/");

                zip.CreateEntryFromFile(file, entryName);
            }

            foreach (var dir in Directory.GetDirectories(folderPath))
            {
                var dirName = Path.GetFileName(dir);
                AddFolderToZip(zip, dir, Path.Combine(zipPath, dirName));
            }
        }
    }

}
