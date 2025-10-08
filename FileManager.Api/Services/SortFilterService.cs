using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileManager.Api.Data;
using FileManager.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace FileManager.Api.Services
{
    public class SortFilterService
    {
        private readonly AppDbContext _context;

        public SortFilterService(AppDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<FileMetadata>> SortByExtensionAsync(bool ascending)
        {
            var query = _context.FileMetadata.AsQueryable();
            
            return ascending
                ? await query.OrderBy(f => f.Type).ToListAsync()
                : await query.OrderByDescending(f => f.Type).ToListAsync();
        }

        public List<FileMetadata> FilterByType(List<FileMetadata> files, List<string> types)
        {
            if (types == null || !types.Any())
                return files;

            return files.Where(f => types.Contains(f.Type)).ToList();
        }

        public async Task<List<FileMetadata>> SortAndFilterAsync(bool? ascending, List<string> types)
        {
            List<FileMetadata> sorted;

            if (ascending.HasValue)
            {
                sorted = await SortByExtensionAsync(ascending.Value);
            }
            else
            {
                sorted = await _context.FileMetadata.ToListAsync();
            }

            if (types != null && types.Any())
            {
                return FilterByType(sorted, types);
            }

            return sorted;
        }

        public async Task<List<FileMetadata>> GetAllFilesForUserAsync(long userId)
        {
            return await _context.FileMetadata
                .Where(f => f.UploaderId == userId)
                .ToListAsync();
        }
    }
}