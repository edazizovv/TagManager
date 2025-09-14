using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using System.Xml.Linq;
using Dapper;
using Npgsql;
using System.Data;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Collections.Generic;

namespace VideoManager.Models
{

    public class Realm
    {
        [Required(ErrorMessage = "Name required")]
        [RegularExpression("^[a-z_]{2,3}$", ErrorMessage = "String must contain only a-z literals or underscore and be 2-3 characters length")]
        public string name { get; set; }
        [Required(ErrorMessage = "View required")]
        [RegularExpression("^[a-zA-Z_:\\\\]{3,100}$", ErrorMessage = "String must contain only a-z literals or underscore or slashes and be 3-100 characters length")]
        public string view { get; set; }
    }

    public class Pizza
    {
        public string realm { get; set; }
        public int id { get; set; }
        public string _name { get; set; }
        public string _author { get; set; }
        public string thumbnail { get; set; }
        public string _link { get; set; }

        public List<string> Tags { get; set; }
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
        // public Task<bool> CreatePizza(Pizza pizza);
        public Task<List<Pizza>> GetPizzaList(List<string> filterTags);
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

        /*
        public async Task<bool> CreatePizza(Pizza pizza)
        {
            var result = await _dbService.Insert<int>("INSERT INTO public.pizzas (id, name, description) VALUES (@Id, @Name, @Description)", pizza);
            return true;
        }
        */

        public async Task<List<Pizza>> GetPizzaList(List<string> filterTags)
        {
            if (filterTags.Count > 0)
                {
                var tagSet = "(" + string.Join(", ", filterTags.Select(t => "'" + t + "'")) + ")";
                /*
                string query = "SELECT x.id FROM " +
                    "( " +
                    "SELECT a.id " +
                          ", a.n " +
                    "FROM " +
                    "( " +
                    "SELECT b.id " +
                         ", SUM(CASE WHEN b.tag IN " + tagSet + " THEN 1 ELSE 0 END) AS n " +
                    "FROM public.pizzatag AS b " +
                    "GROUP BY b.id " +
                    ") AS a " +
                    "WHERE a.n = @N " +
                    ") AS x";
                */
                string query = "SELECT DISTINCT x.id " +
                    "FROM public.pizzatag AS x " +
                    "WHERE x.tag IN " + tagSet +
                    ";";
                var pizzaIds = await _dbService.GetAll<string>(
                    query, new {  });
                List<Pizza> pizzaList = default!;
                if (pizzaIds.Count > 0)
                {
                    string idSet = "(" + string.Join(", ", pizzaIds.Select(t => t)) + ")";
                    string pizzaQuery = "SELECT p.realm " + 
                        "\t, p.\"id\" " +
                        "\t, p.\"_name\" " +
                        "\t, p._author " +
                        "\t, '\\base\\' || r.name || '\\thmb\\' || p.thumbnail as thumbnail " + 
                        "\t, p._link " +
                        "FROM " + 
                        "( " + 
                        "SELECT realm " +
                        "\t, \"id\" " + 
                        "\t, \"_name\" " +
                        "\t, _author " +
                        "\t, thumbnail " +
                        "\t, _link " +
                        "FROM public.pizzas " +
                        "WHERE id IN " + idSet +
                        ") as p " + 
                        "LEFT JOIN " + 
                        "( " + 
                        "SELECT * " + 
                        "FROM public.accessories " +
                        ") as r " + 
                        "ON p.realm = r.name " + 
                        ";";
                    pizzaList = await _dbService.GetAll<Pizza>(pizzaQuery, new { });
                }
                else
                {
                    pizzaList = default!;
                }
                return pizzaList;
            }
            else
            {
                string pizzaQuery = "SELECT p.realm " +
                    "\t, p.\"id\" " +
                    "\t, p.\"_name\" " +
                    "\t, p._author " +
                    "\t, '\\base\\' || r.name || '\\thmb\\' || p.thumbnail as thumbnail " +
                    "\t, p._link " +
                    "FROM " +
                    "( " +
                    "SELECT realm " +
                    "\t, \"id\" " +
                    "\t, \"_name\" " +
                    "\t, _author " +
                    "\t, thumbnail " +
                    "\t, _link " +
                    "FROM public.pizzas " +
                    ") as p " +
                    "LEFT JOIN " +
                    "( " +
                    "SELECT * " +
                    "FROM public.accessories " +
                    ") as r " +
                    "ON p.realm = r.name " +
                    ";";
                var pizzaList = await _dbService.GetAll<Pizza>(pizzaQuery, new { });
                return pizzaList;
            }
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
        public Task<bool> CreateTag(string tag);
        public Task<List<string>> GetTagList();
        public Task<bool> DeleteTag(string tag);
    }

    public class TagService : ITagService
    {
        private readonly IDbService _dbService;

        public TagService(IDbService dbService)
        {
            _dbService = dbService;
        }

        public async Task<bool> CreateTag(string tag)
        {
            var result = await _dbService.Insert<int>("INSERT INTO public.tags (tag) VALUES (@Tag)", new { Tag = tag });
            return true;
        }

        public async Task<List<string>> GetTagList()
        {
            var tagList = await _dbService.GetAll<string>("SELECT * FROM public.tags", new { });
            return tagList;
        }

        public async Task<bool> DeleteTag(string tag)
        {
            var deleteTag = await _dbService.Delete<int>("DELETE FROM public.tags WHERE tag=@Tag", new { Tag = tag });
            return true;
        }

    }

    public interface IRealmService
    {
        public Task<bool> CreateRealm(string realmName, string realmView);
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

        public async Task<bool> CreateRealm(string realmName, string realmView)
        {
            var result = await _dbService.Insert<int>(
                "INSERT INTO public.accessories (name, view) " + 
                "VALUES (@RealmName, @RealmView)", 
                new { RealmName = realmName, RealmView = realmView }
                );
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
        public Task<bool> CreateTag(int pizzaId, string tag);
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

        public async Task<bool> CreateTag(int pizzaId, string tag)
        {
            var result = await _dbService.Insert<int>($"INSERT INTO public.pizzatag (id, tag) VALUES (@pizzaId, @tag)", new { pizzaId = pizzaId, tag = tag });
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

}
