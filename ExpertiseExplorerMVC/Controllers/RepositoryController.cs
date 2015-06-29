namespace ExpertiseExplorer.MVC.Controllers
{
    using System.Web.Mvc;
    using ExpertiseExplorer.ExpertiseDB;
    using ExpertiseExplorer.MVC.Models;

    public class RepositoryController : Controller
    {
        public ActionResult Index(int id)
        {
            using (var entities = new ExpertiseDBEntities())
            {
                var repository = entities.Repositorys.Find(id);
                if (repository == null)
                    return HttpNotFound();

                var result = new RepositoryViewModel
                    {
                        Id = repository.RepositoryId,
                        Name = repository.Name,
                        Developers = entities.GetTopDevelopersForRepository(repository.RepositoryId, 10)
                    };

                return View(result);
            }
        }

        public ActionResult ByDeveloper(int id)
        {
            using (var entities = new ExpertiseDBEntities())
            {
                var repository = entities.Repositorys.Find(id);
                if (repository == null)
                    return HttpNotFound();

                var result = new RepositoryViewModel
                {
                    Id = repository.RepositoryId,
                    Name = repository.Name,
                    Developers = entities.GetTopDevelopersForRepository(repository.RepositoryId)
                };

                return View(result);
            }
        }

        public ActionResult ByArtifact(int id, string active = "a")
        {
            using (var entities = new ExpertiseDBEntities())
            {
                var repository = entities.Repositorys.Find(id);
                if (repository == null)
                    return HttpNotFound();

                var result = new ListArtifactsViewModel
                    {
                        ActiveChar = active,
                        Artifacts = entities.GetFilesForRepositoryThatStartWithChar(id, active.ToLower()),
                        Name = repository.Name,
                        RepositoryId = id
                    };

                return View(result);
            }
        }

        public ActionResult GetOrphans(int id)
        {
            using (var entities = new ExpertiseDBEntities())
            {
                var repository = entities.Repositorys.Find(id);
                if (repository == null)
                    return HttpNotFound();

                var potentialOrphans = entities.GetArtifactsWithJustOneExpertForRepository(repository.RepositoryId);

                var result = new OrphansViewModel
                    {
                        PotentialOrphans = potentialOrphans,
                        RepositoryId = repository.RepositoryId,
                        RepositoryName = repository.Name
                    };

                return View(result);
            }
        }

        public PartialViewResult ListArtifacts(int id, string active)
        {
            using (var entities = new ExpertiseDBEntities())
            {
                var repository = entities.Repositorys.Find(id);

                var result = new ListArtifactsViewModel
                    {
                        ActiveChar = active,
                        Artifacts = entities.GetFilesForRepositoryThatStartWithChar(id, active.ToLower()),
                        Name = repository.Name,
                        RepositoryId = id
                    };

                return PartialView("p_ArtifactsTable", result);
            }
        }
    }
}
