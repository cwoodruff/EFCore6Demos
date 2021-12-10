﻿// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Linq;
using Chinook.Domain.Entities;
using compiled_query.Model;
using Microsoft.EntityFrameworkCore;

public class Program
{
    private static ChinookContext _context;

    private static void Main()
    {
        var builder = new DbContextOptionsBuilder<ChinookContext>();
        builder.UseSqlServer(
            "Server=.;Database=Chinook;Trusted_Connection=True;Application Name=ChinookASPNETCoreAPINTier");

        var dbContextOptions = builder.Options;
        _context = new ChinookContext(dbContextOptions);

        // Warm up
        //var artist = _context.Artists.First();

        RunTest(
            albumIDs =>
            {
                foreach (var id in albumIDs)
                {
                    // Use a regular auto-compiled query
                    var artist = _context.Artists.FirstOrDefault(a => a.Id == id);
                }
            },
            name: "Regular");

        RunTest(
            albumIDs =>
            {
                // Create explicit compiled query
                var query = EF.CompileQuery((ChinookContext context, int id)
                    => _context.Artists.FirstOrDefault(a => a.Id == id));

                foreach (var id in albumIDs)
                {
                    // Invoke the compiled query
                    var artist = query(_context, id);
                }
            },
            name: "Compiled");

        RunTest(
            albumIDs =>
            {
                // Create explicit compiled query
                var query = EF.CompileAsyncQuery((ChinookContext context, int id)
                    => context.Artists.FirstOrDefault(a => a.Id == id));

                foreach (var id in albumIDs)
                {
                    // Invoke the async compiled query
                    var artist = query(_context, id).Result;
                }
            },
            name: "Async Compiled");
        
        RunTest(
            albumIDs =>
            {
                foreach (var id in albumIDs)
                {
                    // Invoke the compiled query from DBContext
                    var artist = _context.GetArtist(id);
                }
            },
            name: "DBContext Compiled");
        
        RunTest(
            albumIDs =>
            {
                foreach (var id in albumIDs)
                {
                    // Invoke the compiled async query from DBContext
                    var artist = _context.GetArtistAsync(id).Result;
                }
            },
            name: "DBContext Async Compiled");
    }

    private static void RunTest(Action<int[]> test, string name)
    {
        var albumIDs = GetAlbumIDs(500);
        var stopwatch = new Stopwatch();

        stopwatch.Start();

        test(albumIDs);

        stopwatch.Stop();

        Console.WriteLine($"{name}:  {stopwatch.ElapsedMilliseconds.ToString(),4}ms");
    }

    private static int[] GetAlbumIDs(int count)
    {
       
        IQueryable<Album> albums = Queryable.Take(_context.Albums, count);

        return albums.AsEnumerable().Select(i => i.Id).ToArray();
    }
}