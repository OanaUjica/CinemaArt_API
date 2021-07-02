﻿using AutoMapper;
using Lab1_.NET.Data;
using Lab1_.NET.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lab1_.NET.ErrorHandling;
using Lab1_.NET.Models;

namespace Lab1_.NET.Services
{
    public class MoviesService : IMoviesService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public MoviesService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        
        public async Task<ServiceResponse<PaginatedResultSet<MovieViewModel>, IEnumerable<EntityError>>> GetMovies(int? page = 1, int? perPage = 5)
        {
            var movies = await _context.Movies
                .OrderBy(m => m.Title)
                .Skip((page.Value - 1) * perPage.Value)
                .Take(perPage.Value)
                .Select(m => _mapper.Map<MovieViewModel>(m))
                .ToListAsync();

            if (page == null || page < 1)
            {
                page = 1;
            }
            if (perPage == null || perPage > 100)
            {
                perPage = 20;
            }

            int count = await _context.Movies.CountAsync();
            var resultSet = new PaginatedResultSet<MovieViewModel>(movies, page.Value, count, perPage.Value);

            var serviceResponse = new ServiceResponse<PaginatedResultSet<MovieViewModel>, IEnumerable<EntityError>>
            {
                ResponseOk = resultSet
            };
            return serviceResponse;
        }

        public async Task<ServiceResponse<MovieViewModel, string>> GetMovie(int id)
        {
            var serviceResponse = new ServiceResponse<MovieViewModel, string>();
            var movie = await _context.Movies
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync();

            var movieResponse = _mapper.Map<MovieViewModel>(movie);
            serviceResponse.ResponseOk = movieResponse;
            return serviceResponse;
        }

        public async Task<ServiceResponse<PaginatedResultSet<MovieWithReviewsViewModel>, IEnumerable<EntityError>>> GetReviewsForMovie(int id, int? page = 1, int? perPage = 5)
        {
            var moviesWithComments = await _context.Movies
                .Where(m => m.Id == id)
                .Include(m => m.Reviews)
                .OrderBy(m => m.Title)
                .Skip((page.Value - 1) * perPage.Value)
                .Take(perPage.Value)
                .Select(m => _mapper.Map<MovieWithReviewsViewModel>(m))
                .ToListAsync();

            if (page == null || page < 1)
            {
                page = 1;
            }
            if (perPage == null || perPage > 100)
            {
                perPage = 20;
            }

            int count = await _context.Movies.CountAsync();
            var resultSet = new PaginatedResultSet<MovieWithReviewsViewModel>(moviesWithComments, page.Value, count, perPage.Value);

            var serviceResponse = new ServiceResponse<PaginatedResultSet<MovieWithReviewsViewModel>, IEnumerable<EntityError>>
            {
                ResponseOk = resultSet
            };

            return serviceResponse;
        }

        public async Task<ServiceResponse<PaginatedResultSet<MovieViewModel>, IEnumerable<EntityError>>> FilterMoviesByDateAdded(DateTime? fromDate, DateTime? toDate, int? page = 1, int? perPage = 5)
        {
            var serviceResponse = new ServiceResponse<PaginatedResultSet<MovieViewModel>, IEnumerable<EntityError>>();
            var errors = new List<EntityError>();

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                errors.Add(new EntityError { ErrorType = "", Message = "Both dates are required" });
                serviceResponse.ResponseError = errors;
                return serviceResponse;
            }

            if (fromDate >= toDate)
            {
                errors.Add(new EntityError { ErrorType = "", Message = "fromDate is not before toDate" });
                serviceResponse.ResponseError = errors;
                return serviceResponse;
            }

            var filteredMovies = await _context.Movies
                .Where(m => m.DateAdded >= fromDate && m.DateAdded <= toDate)
                .OrderByDescending(m => m.YearOfRelease)
                .Include(m => m.Reviews)
                .Select(m => _mapper.Map<MovieViewModel>(m))
                .Skip((page.Value - 1) * perPage.Value)
                .Take(perPage.Value)
                .ToListAsync();

            if (page == null || page < 1)
            {
                page = 1;
            }
            if (perPage == null || perPage > 100)
            {
                perPage = 20;
            }

            int count = await _context.Movies.CountAsync();
            var resultSet = new PaginatedResultSet<MovieViewModel>(filteredMovies, page.Value, count, perPage.Value);

            serviceResponse.ResponseOk = resultSet;

            return serviceResponse;
        }

        public async Task<ServiceResponse<Movie, IEnumerable<EntityError>>> PostMovie(MovieViewModel movieRequest)
        {
            var movie = _mapper.Map<Movie>(movieRequest);
            _context.Movies.Add(movie);

            var serviceResponse = new ServiceResponse<Movie, IEnumerable<EntityError>>();

            try
            {
                await _context.SaveChangesAsync();
                serviceResponse.ResponseOk = movie;
            }
            catch (Exception e)
            {
                var errors = new List<EntityError>
                {
                    new EntityError { ErrorType = e.GetType().ToString(), Message = e.Message }
                };
                serviceResponse.ResponseError = errors;
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<Review, IEnumerable<EntityError>>> PostReviewForMovie(int movieId, ReviewViewModel commentRequest)
        {
            var serviceResponse = new ServiceResponse<Review, IEnumerable<EntityError>>();

            var commentDB = _mapper.Map<Review>(commentRequest);

            var movie = await _context.Movies
                .Where(m => m.Id == movieId)
                .Include(m => m.Reviews)
                .FirstOrDefaultAsync();

            try
            {
                movie.Reviews.Add(commentDB);
                _context.Entry(movie).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                serviceResponse.ResponseOk = commentDB;
            }
            catch (Exception e)
            {
                var errors = new List<EntityError>
                {
                    new EntityError { ErrorType = e.GetType().ToString(), Message = e.Message }
                };
                serviceResponse.ResponseError = errors;
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<Movie, IEnumerable<EntityError>>> PutMovie(int id, MovieViewModel movieRequest)
        {
            var serviceResponse = new ServiceResponse<Movie, IEnumerable<EntityError>>();

            var movie = _mapper.Map<Movie>(movieRequest);

            try
            {

                _context.Entry(movie).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                serviceResponse.ResponseOk = movie;
            }
            catch (Exception e)
            {
                var errors = new List<EntityError>
                {
                    new EntityError { ErrorType = e.GetType().ToString(), Message = e.Message }
                };
                serviceResponse.ResponseError = errors;
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<bool, IEnumerable<EntityError>>> DeleteMovie(int id)
        {
            var serviceResponse = new ServiceResponse<bool, IEnumerable<EntityError>>();

            try
            {
                var movie = await _context.Movies.FindAsync(id);
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
                serviceResponse.ResponseOk = true;
            }
            catch (Exception e)
            {
                var errors = new List<EntityError>
                {
                    new EntityError { ErrorType = e.GetType().ToString(), Message = e.Message }
                };
                serviceResponse.ResponseError = errors;
            }

            return serviceResponse;
        }

        public bool MovieExists(int id)
        {
            return _context.Movies.Any(e => e.Id == id);
        }
    }
}
