using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgriSmart.Web.Data;
using AgriSmart.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriSmart.Web.Services
{
    public class AdvisoryService
    {
        private readonly AppDbContext _context;

        public AdvisoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetCropNamesAsync()
        {
            return await _context.Advisories
                .Select(a => a.CropName)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<List<AdvisoryRecord>> SearchAsync(DateTime? dateFrom, DateTime? dateTo, string crop)
        {
            var query = _context.Advisories.AsQueryable();

            if (dateFrom.HasValue)
                query = query.Where(a => a.Date.Date >= dateFrom.Value.Date);

            if (dateTo.HasValue)
                query = query.Where(a => a.Date.Date <= dateTo.Value.Date);

            if (!string.IsNullOrWhiteSpace(crop) && !crop.Equals("All Crops", StringComparison.OrdinalIgnoreCase))
                query = query.Where(a => a.CropName == crop);

            return await query.OrderByDescending(a => a.Date).ToListAsync();
        }

        public async Task AddAdvisoryAsync(AdvisoryRecord record)
        {
            if (record == null) return;
            _context.Advisories.Add(record);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAdvisoryAsync(AdvisoryRecord record)
        {
            if (record == null) return;
            var existing = await _context.Advisories.FindAsync(record.Id);
            if (existing != null)
            {
                existing.CropName = record.CropName;
                existing.Date = record.Date;
                existing.Description = record.Description;
                existing.Tag = record.Tag;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAdvisoryAsync(int id)
        {
            var existing = await _context.Advisories.FindAsync(id);
            if (existing != null)
            {
                _context.Advisories.Remove(existing);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetTotalAdvisoriesAsync()
        {
            return await _context.Advisories.CountAsync();
        }
    }
}

