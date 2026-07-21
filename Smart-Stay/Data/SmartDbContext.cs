using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Models;

namespace Smart_Stay.Data;

public partial class SmartDbContext : DbContext
{
    public SmartDbContext()
    {
    }

    public SmartDbContext(DbContextOptions<SmartDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<Landlord> Landlords { get; set; }

    public virtual DbSet<ListingApplication> ListingApplications { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<RentalApplication> RentalApplications { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=SMART_DB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("Admin");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("userID");

            entity.HasOne(d => d.User).WithOne(p => p.Admin)
                .HasForeignKey<Admin>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Admin_User");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("Document");

            entity.Property(e => e.DocumentId).HasColumnName("documentID");
            entity.Property(e => e.DocumentPath).HasColumnName("documentPath");
            entity.Property(e => e.DocumentType)
                .HasMaxLength(50)
                .HasColumnName("document_type");
            entity.Property(e => e.ListingApplication).HasColumnName("listingApplication");
            entity.Property(e => e.RentalApplicationId).HasColumnName("rentalApplicationID");
            entity.Property(e => e.UploadDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("upload_date");

            entity.HasOne(d => d.ListingApplicationNavigation).WithMany(p => p.Documents)
                .HasForeignKey(d => d.ListingApplication)
                .HasConstraintName("FK_Document_ListingApplication");

            entity.HasOne(d => d.RentalApplication).WithMany(p => p.Documents)
                .HasForeignKey(d => d.RentalApplicationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Document_rentalApplication");
        });

        modelBuilder.Entity<Landlord>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("Landlord");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("userID");
            entity.Property(e => e.VerificationStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending")
                .HasColumnName("verification_status");

            entity.HasOne(d => d.User).WithOne(p => p.Landlord)
                .HasForeignKey<Landlord>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Landlord_User");
        });

        modelBuilder.Entity<ListingApplication>(entity =>
        {
            entity.ToTable("ListingApplication");

            entity.Property(e => e.ListingApplicationId).HasColumnName("listingApplicationID");
            entity.Property(e => e.AdminId).HasColumnName("adminID");
            entity.Property(e => e.ApplicationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("application_date");
            entity.Property(e => e.ApplicationStatus)
                .HasMaxLength(50)
                .HasColumnName("application_status");
            entity.Property(e => e.LandlordId).HasColumnName("LandlordID");
            entity.Property(e => e.PropertyId).HasColumnName("propertyID");

            entity.HasOne(d => d.Admin).WithMany(p => p.ListingApplications)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ListingApplication_Admin");

            entity.HasOne(d => d.Landlord).WithMany(p => p.ListingApplications)
                .HasForeignKey(d => d.LandlordId)
                .HasConstraintName("FK_ListingApplication_Landlord");

            entity.HasOne(d => d.Property).WithMany(p => p.ListingApplications)
                .HasForeignKey(d => d.PropertyId)
                .HasConstraintName("FK_ListingApplication_Property");
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.PropertyId).HasName("PK_Property");

            entity.Property(e => e.PropertyId).HasColumnName("propertyID");
            entity.Property(e => e.Bathrooms).HasColumnName("bathrooms");
            entity.Property(e => e.Bedrooms).HasColumnName("bedrooms");
            entity.Property(e => e.DateListed)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("date_listed");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.LandlordId).HasColumnName("landlordID");
            entity.Property(e => e.Location)
                .HasMaxLength(50)
                .HasColumnName("location");
            entity.Property(e => e.Price)
                .HasColumnType("money")
                .HasColumnName("price");
            entity.Property(e => e.PropertyType)
                .HasMaxLength(50)
                .HasColumnName("propertyType");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .HasColumnName("title");

            entity.HasOne(d => d.Landlord).WithMany(p => p.Properties)
                .HasForeignKey(d => d.LandlordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Property_Landlord");
        });

        modelBuilder.Entity<RentalApplication>(entity =>
        {
            entity.ToTable("rentalApplication");

            entity.Property(e => e.RentalApplicationId).HasColumnName("rentalApplicationID");
            entity.Property(e => e.ApplicationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("application_date");
            entity.Property(e => e.IdNumber)
                .HasMaxLength(18)
                .HasColumnName("Id_Number");
            entity.Property(e => e.LandlordId).HasColumnName("landlordID");
            entity.Property(e => e.PropertyId).HasColumnName("propertyID");
            entity.Property(e => e.RentalApplicationStatus)
                .HasMaxLength(50)
                .HasColumnName("rentalApplicationStatus");
            entity.Property(e => e.TenantId).HasColumnName("tenantID");

            entity.HasOne(d => d.Landlord).WithMany(p => p.RentalApplications)
                .HasForeignKey(d => d.LandlordId)
                .HasConstraintName("FK_rentalApplication_Landlord");

            entity.HasOne(d => d.Property).WithMany(p => p.RentalApplications)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_rentalApplication_Property");

            entity.HasOne(d => d.Tenant).WithMany(p => p.RentalApplications)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_rentalApplication_Tenant1");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("review");

            entity.Property(e => e.ReviewId).HasColumnName("reviewID");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.PropertyId).HasColumnName("propertyID");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.ReviewDate)
                .HasDefaultValueSql("(CONVERT([date],getdate()))")
                .HasColumnName("reviewDate");
            entity.Property(e => e.TenantId).HasColumnName("tenantID");

            entity.HasOne(d => d.Property).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_review_Property");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_review_Tenant");
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("Tenant");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("userID");
            entity.Property(e => e.EmploymentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Employed")
                .HasColumnName("employment_status");

            entity.HasOne(d => d.User).WithOne(p => p.Tenant)
                .HasForeignKey<Tenant>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tenant_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.DateRegistered)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("date_Registered");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasColumnName("firstName");
            entity.Property(e => e.Password)
                .HasMaxLength(15)
                .HasColumnName("password");
            entity.Property(e => e.PhoneNo)
                .HasMaxLength(10)
                .HasColumnName("phoneNo");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SurName)
                .HasMaxLength(50)
                .HasColumnName("surName");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
