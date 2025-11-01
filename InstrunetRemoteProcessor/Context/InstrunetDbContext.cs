using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using InstrunetBackend.Server.InstrunetModels;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace InstrunetBackend.Server.Context;

public partial class InstrunetDbContext : DbContext
{
    public InstrunetDbContext()
    {
    }

    public InstrunetDbContext(DbContextOptions<InstrunetDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<InstrunetEntry> InstrunetEntries { get; set; }

    public virtual DbSet<Playlist> Playlists { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vote> Votes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql(new Func<string>(() => { 
            using var memstream = new MemoryStream();
            Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("InstrunetBackend.Server.InstrunetDbSecret")!.CopyTo(memstream);
            return Encoding.UTF8.GetString(memstream.ToArray()); 
        })(), Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.4.3-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Uuid).HasName("PRIMARY");

            entity.Property(e => e.Uuid)
                .HasMaxLength(36)
                .HasColumnName("uuid");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Master)
                .HasMaxLength(36)
                .HasColumnName("master");
            entity.Property(e => e.Poster)
                .HasMaxLength(36)
                .HasColumnName("poster");
        });

        modelBuilder.Entity<InstrunetEntry>(entity =>
        {
            entity.HasKey(e => e.Uuid).HasName("PRIMARY");

            entity.ToTable("instrunet_entry");

            entity.HasIndex(e => e.Uuid, "uuid").IsUnique();

            entity.Property(e => e.Uuid)
                .HasMaxLength(36)
                .HasColumnName("uuid");
            entity.Property(e => e.AlbumName)
                .HasColumnType("text")
                .HasColumnName("album_name");
            entity.Property(e => e.Albumcover)
                .HasComment("Coverart for the song. ")
                .HasColumnName("albumcover");
            entity.Property(e => e.Artist)
                .HasComment("Artist wrote the song.")
                .HasColumnType("text")
                .HasColumnName("artist");
            entity.Property(e => e.Databinary)
                .HasComment("Binary data of the song. ")
                .HasColumnName("databinary");
            entity.Property(e => e.Email)
                .HasColumnType("text")
                .HasColumnName("email");
            entity.Property(e => e.Epoch).HasColumnName("epoch");
            entity.Property(e => e.Kind)
                .HasComment("Kind of the instrumental.")
                .HasColumnName("kind");
            entity.Property(e => e.LinkTo)
                .HasColumnType("text")
                .HasColumnName("link_to");
            entity.Property(e => e.SongName)
                .HasColumnType("text")
                .HasColumnName("song_name");
            entity.Property(e => e.User)
                .HasMaxLength(36)
                .HasComment("Who'd uploaded")
                .HasColumnName("user");
        });

        modelBuilder.Entity<Playlist>(entity =>
        {
            entity.HasKey(e => e.Uuid).HasName("PRIMARY");

            entity.ToTable("playlist");

            entity.Property(e => e.Uuid)
                .HasMaxLength(36)
                .HasColumnName("uuid");
            entity.Property(e => e.Content)
                .HasColumnType("json")
                .HasColumnName("content");
            entity.Property(e => e.Owner)
                .HasMaxLength(36)
                .HasColumnName("owner");
            entity.Property(e => e.Private).HasColumnName("private");
            entity.Property(e => e.Title)
                .HasColumnType("text")
                .HasColumnName("title");
            entity.Property(e => e.Tmb).HasColumnName("tmb");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Uuid).HasName("PRIMARY");

            entity.ToTable("user");

            entity.HasIndex(e => e.Uuid, "uuid").IsUnique();

            entity.Property(e => e.Uuid)
                .HasMaxLength(36)
                .HasColumnName("uuid");
            entity.Property(e => e.Avatar).HasColumnName("avatar");
            entity.Property(e => e.Email)
                .HasColumnType("text")
                .HasColumnName("email");
            entity.Property(e => e.Password)
                .HasColumnType("text")
                .HasColumnName("password");
            entity.Property(e => e.Time).HasColumnName("time");
            entity.Property(e => e.Username)
                .HasColumnType("text")
                .HasColumnName("username");
        });

        modelBuilder.Entity<Vote>(entity =>
        {
            entity.HasKey(e => e.Uuid).HasName("PRIMARY");

            entity.Property(e => e.Uuid)
                .HasMaxLength(36)
                .HasColumnName("uuid");
            entity.Property(e => e.IsUpvote).HasColumnName("isUpvote");
            entity.Property(e => e.Master)
                .HasMaxLength(36)
                .HasColumnName("master");
            entity.Property(e => e.User)
                .HasMaxLength(36)
                .HasColumnName("user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
