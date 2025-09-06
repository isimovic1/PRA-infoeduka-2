using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Models;

public partial class Infoeduka2Context : DbContext
{
    public Infoeduka2Context()
    {
    }

    public Infoeduka2Context(DbContextOptions<Infoeduka2Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseStudent> CourseStudents { get; set; }

    public virtual DbSet<CourseTeacher> CourseTeachers { get; set; }

    public virtual DbSet<FileAsset> FileAssets { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<ImportBatch> ImportBatches { get; set; }

    public virtual DbSet<ImportRow> ImportRows { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder.UseSqlServer("name=ConnectionStrings:ConnString");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Course__3214EC0738785F6A");

            entity.ToTable("Course");

            entity.HasIndex(e => e.Name, "UQ_Course_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
        });

        modelBuilder.Entity<CourseStudent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CourseSt__3214EC070C2E83FC");

            entity.ToTable("CourseStudent");

            entity.HasIndex(e => e.CourseId, "IX_CourseStudent_CourseId");

            entity.HasIndex(e => e.StudentId, "IX_CourseStudent_StudentId");

            entity.HasIndex(e => new { e.CourseId, e.StudentId }, "UQ_CourseStudent").IsUnique();

            entity.HasOne(d => d.Course).WithMany(p => p.CourseStudents)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CourseStudent_Course");

            entity.HasOne(d => d.Student).WithMany(p => p.CourseStudents)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CourseStudent_User");
        });

        modelBuilder.Entity<CourseTeacher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CourseTe__3214EC07BB587300");

            entity.ToTable("CourseTeacher");

            entity.HasIndex(e => e.CourseId, "IX_CourseTeacher_CourseId");

            entity.HasIndex(e => e.TeacherId, "IX_CourseTeacher_TeacherId");

            entity.HasIndex(e => new { e.CourseId, e.TeacherId }, "UQ_CourseTeacher").IsUnique();

            entity.HasOne(d => d.Course).WithMany(p => p.CourseTeachers)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CourseTeacher_Course");

            entity.HasOne(d => d.Teacher).WithMany(p => p.CourseTeachers)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CourseTeacher_User");
        });

        modelBuilder.Entity<FileAsset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FileAsse__3214EC070CA0F418");

            entity.ToTable("FileAsset");

            entity.HasIndex(e => e.CourseId, "IX_FileAsset_CourseId");

            entity.HasIndex(e => e.UploadedAt, "IX_FileAsset_UploadedAt");

            entity.HasIndex(e => e.UploadedById, "IX_FileAsset_UploadedById");

            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.FileName).HasMaxLength(260);
            entity.Property(e => e.StoredPath).HasMaxLength(400);
            entity.Property(e => e.UploadedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Course).WithMany(p => p.FileAssets)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FileAsset_Course");

            entity.HasOne(d => d.UploadedBy).WithMany(p => p.FileAssets)
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FileAsset_Uploader");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Grade__3214EC070345AAAC");

            entity.ToTable("Grade");

            entity.HasIndex(e => e.TeacherId, "IX_Grade_TeacherId");

            entity.HasIndex(e => e.SubmissionId, "UX_Grade_SubmissionId").IsUnique();

            entity.Property(e => e.GradedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Points).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Submission).WithOne(p => p.Grade)
                .HasForeignKey<Grade>(d => d.SubmissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Grade_Submission");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Grades)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Grade_Teacher");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Group__3214EC0700913973");

            entity.ToTable("Group");

            entity.HasIndex(e => e.Name, "UQ__Group__737584F66365C7DC").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<ImportBatch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ImportBa__3214EC071CC20201");

            entity.ToTable("ImportBatch");

            entity.HasIndex(e => e.CreatedAt, "IX_ImportBatch_CreatedAt");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SourceFileName).HasMaxLength(260);

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.ImportBatches)
                .HasForeignKey(d => d.CreatedById)
                .HasConstraintName("FK_ImportBatch_User");
        });

        modelBuilder.Entity<ImportRow>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ImportRo__3214EC0785054983");

            entity.ToTable("ImportRow");

            entity.HasIndex(e => e.ImportBatchId, "IX_ImportRow_ImportBatchId");

            entity.Property(e => e.Error).HasMaxLength(1000);

            entity.HasOne(d => d.ImportBatch).WithMany(p => p.ImportRows)
                .HasForeignKey(d => d.ImportBatchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ImportRow_Batch");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC07AD4D4AE2");

            entity.ToTable("Notification");

            entity.HasIndex(e => e.CreatedAt, "IX_Notification_CreatedAt");

            entity.HasIndex(e => new { e.ToUserId, e.IsRead, e.CreatedAt }, "IX_Notification_ToUserId");

            entity.Property(e => e.Body).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Link).HasMaxLength(400);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.FromUser).WithMany(p => p.NotificationFromUsers)
                .HasForeignKey(d => d.FromUserId)
                .HasConstraintName("FK_Notification_FromUser");

            entity.HasOne(d => d.ToUser).WithMany(p => p.NotificationToUsers)
                .HasForeignKey(d => d.ToUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notification_ToUser");
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Submissi__3214EC07142D470E");

            entity.ToTable("Submission");

            entity.HasIndex(e => e.CourseId, "IX_Submission_CourseId");

            entity.HasIndex(e => e.StudentId, "IX_Submission_StudentId");

            entity.HasIndex(e => new { e.CourseId, e.StudentId, e.FileAssetId }, "UQ_Submission").IsUnique();

            entity.HasOne(d => d.Course).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Submission_Course");

            entity.HasOne(d => d.FileAsset).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.FileAssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Submission_FileAsset");

            entity.HasOne(d => d.Student).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Submission_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3214EC07E0E81069");

            entity.ToTable("User");

            entity.HasIndex(e => e.GroupId, "IX_User_GroupId");

            entity.HasIndex(e => e.Email, "UQ__User__A9D10534CEB7E6EF").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.IsFirstLogin).HasDefaultValue(true);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);

            entity.HasOne(d => d.Group).WithMany(p => p.Users)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK_User_Group");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
