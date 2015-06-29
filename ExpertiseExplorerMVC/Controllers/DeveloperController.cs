namespace ExpertiseExplorer.MVC.Controllers
{
    using System.Web.Mvc;
    using ExpertiseExplorer.ExpertiseDB;
    using ExpertiseExplorer.MVC.Models;

    public class DeveloperController : Controller
    {
        public ActionResult Index(int id)
        {
            using (var repository = new ExpertiseDBEntities())
            {
                var developer = repository.Developers.Find(id);
                if (developer == null)
                    return HttpNotFound();

                return View(new DeveloperViewModel { Artifacts = repository.GetArtifactsForDeveloper(id), Id = id, Name = developer.Name });
            }
        }

        public ActionResult GetSoleExpertList(int id)
        {
            using (var repository = new ExpertiseDBEntities())
            {
                var developer = repository.Developers.Find(id);
                if (developer == null)
                    return HttpNotFound();

                return View(new DeveloperViewModel { Artifacts = repository.GetSoleExpertArtifactsForDeveloper(id), Id = id, Name = developer.Name });
            }
        }
    }
}
