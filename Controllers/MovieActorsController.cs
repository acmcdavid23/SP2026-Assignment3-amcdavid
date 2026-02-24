using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SP2026_Assignment3_amcdavid.Data;
using SP2026_Assignment3_amcdavid.Models;

namespace SP2026_Assignment3_amcdavid.Controllers
{
    public class MovieActorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MovieActorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var movieActors = await _context.MovieActors
                .Include(ma => ma.Movie)
                .Include(ma => ma.Actor)
                .ToListAsync();
            return View(movieActors);
        }

        public IActionResult Create()
        {
            ViewBag.Movies = new SelectList(_context.Movies, "Id", "Title");
            ViewBag.Actors = new SelectList(_context.Actors, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieActor movieActor)
        {
            var exists = await _context.MovieActors
                .AnyAsync(ma => ma.MovieId == movieActor.MovieId && ma.ActorId == movieActor.ActorId);

            if (!exists)
            {
                _context.Add(movieActor);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int movieId, int actorId)
        {
            var movieActor = await _context.MovieActors
                .FindAsync(movieId, actorId);

            if (movieActor != null)
            {
                _context.MovieActors.Remove(movieActor);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
