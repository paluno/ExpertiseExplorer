﻿namespace ExpertiseExplorer.MVC.Controllers
{
    using System.Web.Mvc;
    using ExpertiseExplorer.ExpertiseDB;
    using ExpertiseExplorer.MVC.Models;

    public class ArtifactController : Controller
    {
        public ActionResult Index(int id)
        {
            using (var repository = new ExpertiseDBEntities())
            {
                var artifact = repository.Artifacts.Find(id);
                if (artifact == null)
                    return HttpNotFound();

                return View(new ArtifactViewModel
                            {
                                Developers = repository.GetDevelopersForArtifact(id),
                                Id = artifact.ArtifactId,
                                Name = artifact.Name
                            });
            }
        }
    }
}
