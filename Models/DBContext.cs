using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlmaDUploader.Models
{
    public class IngestsContext : DbContext
    {
        public DbSet<Ingest> Ingests { get; set; }
        public DbSet<IngestFile> IngestFiles { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Ingest>().HasMany(e => e.Files).WithOptional(s => s.Ingest).WillCascadeOnDelete(true);
            modelBuilder.Entity<IngestFile>().HasOptional(e => e.Ingest);
            modelBuilder.ComplexType<Metadata>().Property(p => p.Serialized).HasColumnName("metadata");
            modelBuilder.ComplexType<Metadata>()
                .Ignore(p => p.Author)
                .Ignore(p => p.ISBN)
                .Ignore(p => p.Notes)
                .Ignore(p => p.PublicationDate)
                .Ignore(p => p.Publisher)
                .Ignore(p => p.Title);
        }

        public override int SaveChanges()
        {
            //List<IngestFile> changedFiles = new List<IngestFile>();
            // raise an event whenever a file is changed
            List<IngestFile> changedFiles = 
                this.ChangeTracker.Entries().Where(e => (e.State == EntityState.Modified || e.State == EntityState.Added) 
                    && e.Entity is IngestFile).Select(e => e.Entity).Cast<IngestFile>().ToList();
            /*
            foreach (var dbEntity in this.ChangeTracker.Entries())
            {
                if ((dbEntity.State == EntityState.Modified || dbEntity.State == EntityState.Added) &&
                    dbEntity.Entity is IngestFile)
                    changedFiles.Add((IngestFile)dbEntity.Entity);
            }
            */
            int resp = base.SaveChanges();

            changedFiles.ForEach(f => FileChanged(f, e));

            return resp;
        }

        public event FileChangedHandler FileChanged;
        public EventArgs e = null;
        public delegate void FileChangedHandler(IngestFile f, EventArgs e);

    }
}
