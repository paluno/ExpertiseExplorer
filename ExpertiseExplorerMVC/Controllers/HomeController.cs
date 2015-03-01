namespace ExpertiseExplorerMVC.Controllers
{
    using System.Linq;
    using System.Web.Mvc;
    using ExpertiseDB;

    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (var repository = new ExpertiseDBEntities())
            {
                var repositories = repository.Repositorys.ToList();

                return View(repositories);
            }
        }
    }
}
