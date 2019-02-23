using AspNetCoreWebApp.FileAnalyzer;
using AspNetCoreWebApp.FileEditor;
using AspNetCoreWebApp.Files;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreWebApp
{
    public class FileSystemContext : DbContext
    { 
        public FileSystemContext() { }

        public FileSystemContext(DbContextOptions<FileSystemContext> options) : base(options) {}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileEdit>()
                .HasKey(fc => fc.FileId);

            modelBuilder.Entity<FileAnalysis>()
                .HasKey(fa => fa.FileId);
        }

        public DbSet<File> Files { get; set; }

        public DbSet<FileEdit> FileEdits { get; set; }

        public DbSet<FileAnalysis> FileAnalysis { get; set; }
    }
}
