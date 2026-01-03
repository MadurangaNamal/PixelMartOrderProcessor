using Microsoft.EntityFrameworkCore;

namespace Shared.Data;

public class PixelMartOrderProcessorDbContext : DbContext
{
    public PixelMartOrderProcessorDbContext(DbContextOptions<PixelMartOrderProcessorDbContext> options)
        : base(options)
    {
    }
}
