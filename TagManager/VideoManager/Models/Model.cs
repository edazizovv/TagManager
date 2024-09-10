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

    public class Pizza
    {
        public int id { get; set; }
        public string name { get; set; }
        public string _name { get; set; }
        public string author { get; set; }
        public string _author { get; set; }
        public string description { get; set; }
        public string _description { get; set; }
        public string link { get; set; }
        public string _link { get; set; }
        public string thumbnail { get; set; }
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
                var pizzaIds = await _dbService.GetAll<string>(
                    query, new { N = filterTags.Count });
                var idSet = "(" + string.Join(", ", pizzaIds.Select(t => t)) + ")";
                var pizzaList = await _dbService.GetAll<Pizza>("SELECT * FROM public.pizzas WHERE id IN " + idSet, new { });
                return pizzaList;
            }
            else
            {
                var pizzaList = await _dbService.GetAll<Pizza>("SELECT * FROM public.pizzas", new { });
                return pizzaList;
            }
        }

        public async Task<Pizza> UpdatePizza(Pizza pizza)
        {
            // TODO: no updates to the pizzas' main fields should be applied, only some specific fields, TBD
            // not used ATM, let it be
            var updatePizza = await _dbService.Update<int>("UPDATE public.pizzas SET name=@Name, description=@Description WHERE id=@Id", pizza);
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
