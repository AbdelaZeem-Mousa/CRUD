using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVCDOTNET5.Models;
using MVCDOTNET5.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NToastNotify;

namespace MVCDOTNET5.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private  List<string> _allowedextation = new List<string> { ".jpg", ".png" };
        private long _maxalllPosterSize = 1048576;
        private readonly IToastNotification _toastNotification;
        public MoviesController(ApplicationDbContext dbContext , IToastNotification toastNotification)
        {
            _dbContext = dbContext;
            _toastNotification = toastNotification;
        }
        public async Task< IActionResult> Index()
        {
            var movies = await _dbContext.Movies.OrderByDescending(q=>q.Rate).ToListAsync();
            return View(movies);
        }
        public async Task<IActionResult> Create()
        {
            var viewmodel = new MovieViewModel
            {
                Genres = await _dbContext.Genres.OrderBy(q=>q.Name).ToListAsync()
            };
            return View("MovieForm", viewmodel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieViewModel movieViewModel)
        {
            if (!ModelState.IsValid)
            {
                movieViewModel.Genres = await _dbContext.Genres.OrderBy(q => q.Name).ToListAsync();
                return View("MovieForm", movieViewModel);

            }
            var files = Request.Form.Files;
            if (!files.Any())
            {
                movieViewModel.Genres = await _dbContext.Genres.OrderBy(q => q.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Please Select Poster");
                return View("MovieForm", movieViewModel);
            }
            var poster = files.FirstOrDefault();
            //var allowedextation = new List<string> { ".jpg", ".png" };
            _allowedextation = new List<string> { ".jpg", ".png" };
            if (!_allowedextation.Contains(Path.GetExtension(poster.FileName).ToLower()))
            {
                movieViewModel.Genres = await _dbContext.Genres.OrderBy(q => q.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Only .jpg , .png Images Is Allowed");
                return View("MovieForm", movieViewModel);
            }
            if (poster.Length > _maxalllPosterSize)
            {
                movieViewModel.Genres = await _dbContext.Genres.OrderBy(q => q.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Poster Cannot be more 1 MB!");
                return View("MovieForm", movieViewModel);
            }
            using var datastreem = new MemoryStream();
            await poster.CopyToAsync(datastreem);
            Movie movie = new Movie
            {
                Id = 0,
                GenreId = movieViewModel.GenreId,
                Poster = datastreem.ToArray(),
                Rate = movieViewModel.Rate,
                StoreLine = movieViewModel.StoreLine,
                Title = movieViewModel.Title,
                Year = movieViewModel.Year

            };
            _dbContext.Movies.Add(movie);
            _dbContext.SaveChanges();
            _toastNotification.AddSuccessToastMessage("Movie Created Success");
            return RedirectToAction(nameof(Index));

        }
        public async Task<IActionResult> Edit(int ? id)
        {
            if (id == null) return BadRequest();

            var movie = await _dbContext.Movies.FindAsync(id);
            if (movie==null) return NotFound();
            var viewmodel = new MovieViewModel
            {
                Id = movie.Id,
                GenreId = movie.GenreId,
                Poster = movie.Poster,
                Rate=movie.Rate,
                StoreLine=movie.StoreLine,
                Title=movie.Title,
                Year = movie.Year,
                Genres = await _dbContext.Genres.OrderBy(q => q.Name).ToListAsync()
            };

            return View("MovieForm", viewmodel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MovieViewModel movieViewModel)
        {
            if (!ModelState.IsValid)
            {
                movieViewModel.Genres = await _dbContext.Genres.OrderBy(q => q.Name).ToListAsync();
                return View("MovieForm", movieViewModel);

            }
            var movieEdit = await _dbContext.Movies.FindAsync(movieViewModel.Id);
            if (movieEdit == null) return NotFound();
            var files = Request.Form.Files;
            if (files.Any())
            {


                var poster = files.FirstOrDefault();
                using var datastreem = new MemoryStream();
                await poster.CopyToAsync(datastreem);
                movieViewModel.Poster = datastreem.ToArray();
                _allowedextation = new List<string> { ".jpg", ".png" };
                if (!_allowedextation.Contains(Path.GetExtension(poster.FileName).ToLower()))
                {
                    movieViewModel.Genres = await _dbContext.Genres.OrderBy(q => q.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Only .jpg , .png Images Is Allowed");
                    return View("MovieForm", movieViewModel);
                }
                if (poster.Length > _maxalllPosterSize)
                {
                    movieViewModel.Genres = await _dbContext.Genres.OrderBy(q => q.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Poster Cannot be more 1 MB!");
                    return View("MovieForm", movieViewModel);
                }

                movieEdit.Poster = datastreem.ToArray();
            }



            movieEdit.GenreId = movieViewModel.GenreId;
            movieEdit.Rate = movieViewModel.Rate;
            movieEdit.StoreLine = movieViewModel.StoreLine;
            movieEdit.Title = movieViewModel.Title;
            movieEdit.Year = movieViewModel.Year;
            _dbContext.SaveChanges();
            _toastNotification.AddSuccessToastMessage("Movie Edited Success");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();

            var movie = await _dbContext.Movies.Include(q=>q.Genre).SingleOrDefaultAsync(q=>q.Id == id);
            if (movie == null) return NotFound();

            return View(movie);
        }
        public async Task<IActionResult> Detete(int? id)
        {
            if (id == null) return BadRequest();

            var movie = await _dbContext.Movies.FindAsync(id);
            if (movie == null) return NotFound();

            _dbContext.Movies.Remove(movie);
            _dbContext.SaveChanges();

            return Ok();
        }
    }
}
