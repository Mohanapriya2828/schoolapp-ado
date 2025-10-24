using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SchoolApp.Models;

public partial class StudentTeacherContext : DbContext
{
    public StudentTeacherContext()
    {
    }

    public StudentTeacherContext(DbContextOptions<StudentTeacherContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=PSILENL057;Database=Student_teacher;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__users__3213E83F0F215D0F");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "UQ__users__AB6E6164B832531A").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdat");
            entity.Property(e => e.Deletedat)
                .HasColumnType("datetime")
                .HasColumnName("deletedat");
            entity.Property(e => e.Department)
                .HasMaxLength(60)
                .HasColumnName("department");
            entity.Property(e => e.Designation)
                .HasMaxLength(50)
                .HasColumnName("designation");
            entity.Property(e => e.Dob).HasColumnName("dob");
            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .HasColumnName("email");
            entity.Property(e => e.Gender)
                .HasMaxLength(20)
                .HasColumnName("gender");
            entity.Property(e => e.Isactive)
                .HasDefaultValue(true)
                .HasColumnName("isactive");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.Passwordhash)
                .HasMaxLength(500)
                .HasColumnName("passwordhash");
            entity.Property(e => e.Phonenumber)
                .HasMaxLength(20)
                .HasColumnName("phonenumber");
            entity.Property(e => e.Profileimageurl)
                .HasMaxLength(500)
                .HasColumnName("profileimageurl");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasColumnName("role");
            entity.Property(e => e.Updatedat)
                .HasColumnType("datetime")
                .HasColumnName("updatedat");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
