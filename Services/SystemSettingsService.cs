using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Services
{
    public class SystemSettingsService
    {
        private readonly ApplicationDbContext _context;

        public SystemSettingsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SystemSettings> GetSettingsAsync()
        {
            var s = await _context.SystemSettings.FirstOrDefaultAsync();
            if (s == null)
            {
                s = new SystemSettings
                {
                    CompanyName = "Zimbotech Memorial Park",
                    SmtpPort = 587,
                    UseSSL = true,
                    MaxBurialsPerDay = 5,
                    MinutesBetweenBurials = 60,
                    LogoPath = "/uploads/company_logo.png"
                };
                _context.SystemSettings.Add(s);
                await _context.SaveChangesAsync();
            }
            return s;
        }

        public async Task UpdateSettingsAsync(SystemSettings updated)
        {
            var existing = await _context.SystemSettings.FirstOrDefaultAsync();
            if (existing == null)
            {
                _context.SystemSettings.Add(updated);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(updated);
            }

            await _context.SaveChangesAsync();
        }
    }
}
