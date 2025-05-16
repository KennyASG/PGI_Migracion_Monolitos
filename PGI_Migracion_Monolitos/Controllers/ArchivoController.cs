using Microsoft.AspNetCore.Mvc;

namespace PGI_Migracion_Monolitos.Controllers;

public class ArchivoController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}