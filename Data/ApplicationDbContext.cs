using Microsoft.EntityFrameworkCore;
using SDP1.Models;

namespace SDP1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DisabilityUser> DisabilityUsers { get; set; }
        public DbSet<Volunteer> Volunteers { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<AdminActivityLog> AdminActivityLogs { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<SavedJob> SavedJobs { get; set; }
        public DbSet<HealthcareProvider> HealthcareProviders { get; set; }
        public DbSet<HealthcareAppointment> HealthcareAppointments { get; set; }
        public DbSet<HealthcareReview> HealthcareReviews { get; set; }

        public DbSet<AccessibilityPlace> AccessibilityPlaces { get; set; }

        public DbSet<VolunteerSupportRequest> VolunteerSupportRequests { get; set; }
        public DbSet<VolunteerChatMessage> VolunteerChatMessages { get; set; }

        public DbSet<TrainingProgram> TrainingPrograms { get; set; }
        public DbSet<TrainingRegistration> TrainingRegistrations { get; set; }
        public DbSet<Scholarship> Scholarships { get; set; }
        public DbSet<ScholarshipApplication> ScholarshipApplications { get; set; }
        public DbSet<AwarenessEvent> AwarenessEvents { get; set; }
        public DbSet<EventRegistration> EventRegistrations { get; set; }
        public DbSet<SavedOpportunity> SavedOpportunities { get; set; }

        public DbSet<LearningVideo> LearningVideos { get; set; }

        public DbSet<CommunityPost> CommunityPosts { get; set; }
        public DbSet<CommunityComment> CommunityComments { get; set; }
        public DbSet<CommunityPostLike> CommunityPostLikes { get; set; }
        public DbSet<CommunityReport> CommunityReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DisabilityUser>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Volunteer>().HasIndex(v => v.Email).IsUnique();
            modelBuilder.Entity<Organization>().HasIndex(o => o.Email).IsUnique();
            modelBuilder.Entity<Admin>().HasIndex(a => a.Email).IsUnique();

           
            modelBuilder.Entity<Job>()
                .HasOne(j => j.Organization)
                .WithMany(o => o.Jobs)
                .HasForeignKey(j => j.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            
            modelBuilder.Entity<JobApplication>()
                .HasIndex(ja => new { ja.JobId, ja.DisabilityUserId })
                .IsUnique();

            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.Job)
                .WithMany(j => j.JobApplications)
                .HasForeignKey(ja => ja.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.DisabilityUser)
                .WithMany(u => u.JobApplications)
                .HasForeignKey(ja => ja.DisabilityUserId)
                .OnDelete(DeleteBehavior.Cascade);

           
            modelBuilder.Entity<SavedJob>()
                .HasIndex(sj => new { sj.JobId, sj.DisabilityUserId })
                .IsUnique();

            modelBuilder.Entity<SavedJob>()
                .HasOne(sj => sj.Job)
                .WithMany(j => j.SavedJobs)
                .HasForeignKey(sj => sj.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SavedJob>()
                .HasOne(sj => sj.DisabilityUser)
                .WithMany(u => u.SavedJobs)
                .HasForeignKey(sj => sj.DisabilityUserId)
                .OnDelete(DeleteBehavior.Cascade);

           
            modelBuilder.Entity<HealthcareProvider>()
                .HasOne(hp => hp.Organization)
                .WithMany(o => o.HealthcareProviders)
                .HasForeignKey(hp => hp.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

           
            modelBuilder.Entity<HealthcareAppointment>()
                .HasOne(ha => ha.HealthcareProvider)
                .WithMany(hp => hp.Appointments)
                .HasForeignKey(ha => ha.HealthcareProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HealthcareAppointment>()
                .HasOne(ha => ha.DisabilityUser)
                .WithMany(u => u.HealthcareAppointments)
                .HasForeignKey(ha => ha.DisabilityUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HealthcareReview>()
                .HasIndex(hr => new { hr.HealthcareProviderId, hr.DisabilityUserId })
                .IsUnique();

            modelBuilder.Entity<HealthcareReview>()
                .HasOne(hr => hr.HealthcareProvider)
                .WithMany(hp => hp.Reviews)
                .HasForeignKey(hr => hr.HealthcareProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HealthcareReview>()
                .HasOne(hr => hr.DisabilityUser)
                .WithMany(u => u.HealthcareReviews)
                .HasForeignKey(hr => hr.DisabilityUserId)
                .OnDelete(DeleteBehavior.Cascade);


            
            modelBuilder.Entity<VolunteerSupportRequest>()
                .HasOne(r => r.RequestedByUser)
                .WithMany(u => u.VolunteerSupportRequests)
                .HasForeignKey(r => r.RequestedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VolunteerSupportRequest>()
                .HasOne(r => r.AssignedVolunteer)
                .WithMany(v => v.AssignedRequests)
                .HasForeignKey(r => r.AssignedVolunteerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<VolunteerChatMessage>()
                .HasOne(m => m.SupportRequest)
                .WithMany(r => r.ChatMessages)
                .HasForeignKey(m => m.VolunteerSupportRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            
            modelBuilder.Entity<TrainingProgram>()
                .HasOne(tp => tp.Organization)
                .WithMany(o => o.TrainingPrograms)
                .HasForeignKey(tp => tp.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TrainingRegistration>()
                .HasIndex(tr => new { tr.TrainingProgramId, tr.DisabilityUserId })
                .IsUnique();

            modelBuilder.Entity<TrainingRegistration>()
                .HasOne(tr => tr.TrainingProgram)
                .WithMany(tp => tp.Registrations)
                .HasForeignKey(tr => tr.TrainingProgramId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TrainingRegistration>()
                .HasOne(tr => tr.DisabilityUser)
                .WithMany(u => u.TrainingRegistrations)
                .HasForeignKey(tr => tr.DisabilityUserId)
                .OnDelete(DeleteBehavior.Cascade);

            
            modelBuilder.Entity<Scholarship>()
                .HasOne(s => s.Organization)
                .WithMany(o => o.Scholarships)
                .HasForeignKey(s => s.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ScholarshipApplication>()
                .HasIndex(sa => new { sa.ScholarshipId, sa.DisabilityUserId })
                .IsUnique();

            modelBuilder.Entity<ScholarshipApplication>()
                .HasOne(sa => sa.Scholarship)
                .WithMany(s => s.Applications)
                .HasForeignKey(sa => sa.ScholarshipId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ScholarshipApplication>()
                .HasOne(sa => sa.DisabilityUser)
                .WithMany(u => u.ScholarshipApplications)
                .HasForeignKey(sa => sa.DisabilityUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AwarenessEvent>()
                .HasOne(ae => ae.Organization)
                .WithMany(o => o.AwarenessEvents)
                .HasForeignKey(ae => ae.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventRegistration>()
                .HasIndex(er => new { er.AwarenessEventId, er.DisabilityUserId })
                .IsUnique();

            modelBuilder.Entity<EventRegistration>()
                .HasOne(er => er.AwarenessEvent)
                .WithMany(ae => ae.Registrations)
                .HasForeignKey(er => er.AwarenessEventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventRegistration>()
                .HasOne(er => er.DisabilityUser)
                .WithMany(u => u.EventRegistrations)
                .HasForeignKey(er => er.DisabilityUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SavedOpportunity>()
                .HasIndex(so => new { so.DisabilityUserId, so.OpportunityType, so.OpportunityId })
                .IsUnique();

            modelBuilder.Entity<SavedOpportunity>()
                .HasOne(so => so.DisabilityUser)
                .WithMany(u => u.SavedOpportunities)
                .HasForeignKey(so => so.DisabilityUserId)
                .OnDelete(DeleteBehavior.Cascade);

            
            modelBuilder.Entity<LearningVideo>()
                .HasIndex(v => v.Category);

            modelBuilder.Entity<LearningVideo>().HasData(
                new LearningVideo
                {
                    Id = 1,
                    Title = "Introduction to Unified English Braille (UEB)",
                    Category = LearningCategories.Braille,
                    Description = "Learn the fundamentals of Unified English Braille, understanding the 6-dot cell system, letter symbols, and tactile reading techniques.",
                    YouTubeUrl = "https://www.youtube.com/watch?v=1n_2hL8N3Jk",
                    Duration = "18 mins",
                    IsPublished = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new LearningVideo
                {
                    Id = 2,
                    Title = "Basic Sign Language Alphabet and Common Phrases",
                    Category = LearningCategories.SignLanguage,
                    Description = "A friendly beginner's guide to fingerspelling the alphabet, daily greetings, and key conversational signs for effective visual communication.",
                    YouTubeUrl = "https://www.youtube.com/watch?v=0FcwzMq4iWg",
                    Duration = "15 mins",
                    IsPublished = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new LearningVideo
                {
                    Id = 3,
                    Title = "Mastering NVDA Screen Reader: Essential Shortcuts",
                    Category = LearningCategories.ComputerSkills,
                    Description = "Comprehensive walkthrough of using the free NonVisual Desktop Access (NVDA) screen reader on Windows for browsing, word processing, and file navigation.",
                    YouTubeUrl = "https://www.youtube.com/watch?v=dEbl5jvLKGQ",
                    Duration = "22 mins",
                    IsPublished = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new LearningVideo
                {
                    Id = 4,
                    Title = "Freelancing Opportunities for People with Disabilities",
                    Category = LearningCategories.Freelancing,
                    Description = "Practical roadmap to setting up remote freelance profiles in data entry, virtual assistance, content writing, and graphic design from home.",
                    YouTubeUrl = "https://www.youtube.com/watch?v=J---aiyznGQ",
                    Duration = "25 mins",
                    IsPublished = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new LearningVideo
                {
                    Id = 5,
                    Title = "Smartphone Accessibility: Android & iOS Features",
                    Category = LearningCategories.DigitalLiteracy,
                    Description = "Explore built-in accessibility suites on modern smartphones, including TalkBack, VoiceOver, Magnifier, Live Captions, and High Contrast displays.",
                    YouTubeUrl = "https://www.youtube.com/watch?v=9No-FiEInLA",
                    Duration = "20 mins",
                    IsPublished = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<AccessibilityPlace>().HasData(
                new AccessibilityPlace
                {
                    Id = 1,
                    PlaceName = "Dhaka Medical College Hospital",
                    PlaceType = "Hospital",
                    Description = "Major government hospital with dedicated accessibility ramps at main OPD, modern elevators, and reserved wheelchair parking.",
                    Address = "Secretariat Road, Ramna",
                    City = "Dhaka",
                    District = "Dhaka",
                    Latitude = 23.7258,
                    Longitude = 90.3976,
                    WheelchairRamp = true,
                    Elevator = true,
                    AccessibleToilet = true,
                    AccessibleParking = true,
                    AccessibilityScore = 95,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new AccessibilityPlace
                {
                    Id = 2,
                    PlaceName = "National Museum of Bangladesh",
                    PlaceType = "Public Park",
                    Description = "Cultural heritage museum featuring flat wide entry ramps, tactile walking floor indicators, and accessible restrooms on each floor.",
                    Address = "Shahbagh Avenue",
                    City = "Dhaka",
                    District = "Dhaka",
                    Latitude = 23.7381,
                    Longitude = 90.3952,
                    WheelchairRamp = true,
                    Elevator = true,
                    AccessibleToilet = true,
                    AccessibleParking = false,
                    AccessibilityScore = 80,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new AccessibilityPlace
                {
                    Id = 3,
                    PlaceName = "Bashundhara City Shopping Complex",
                    PlaceType = "Shopping Mall",
                    Description = "One of South Asia's largest malls with full wheelchair accessibility, wide elevators, accessible toilets on all levels, and underground accessible parking.",
                    Address = "Panthapath, Kawran Bazar",
                    City = "Dhaka",
                    District = "Dhaka",
                    Latitude = 23.7508,
                    Longitude = 90.3912,
                    WheelchairRamp = true,
                    Elevator = true,
                    AccessibleToilet = true,
                    AccessibleParking = true,
                    AccessibilityScore = 95,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new AccessibilityPlace
                {
                    Id = 4,
                    PlaceName = "Kamalapur Central Railway Station",
                    PlaceType = "Transit Station",
                    Description = "Central railway terminal with ground floor platform ramp access and accessible waiting area, but overhead bridge elevators can experience periodic maintenance.",
                    Address = "Kamalapur, Motijheel",
                    City = "Dhaka",
                    District = "Dhaka",
                    Latitude = 23.7317,
                    Longitude = 90.4258,
                    WheelchairRamp = true,
                    Elevator = false,
                    AccessibleToilet = true,
                    AccessibleParking = true,
                    AccessibilityScore = 70,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new AccessibilityPlace
                {
                    Id = 5,
                    PlaceName = "Chittagong Medical College Hospital",
                    PlaceType = "Hospital",
                    Description = "Premier tertiary medical center in southeastern Bangladesh with emergency ramps, accessible diagnostic labs, and dedicated elevator banks.",
                    Address = "57 K.B. Fazlul Kader Road",
                    City = "Chittagong",
                    District = "Chittagong",
                    Latitude = 22.3592,
                    Longitude = 91.8215,
                    WheelchairRamp = true,
                    Elevator = true,
                    AccessibleToilet = true,
                    AccessibleParking = true,
                    AccessibilityScore = 90,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new AccessibilityPlace
                {
                    Id = 6,
                    PlaceName = "Agrabad Government Commercial Center",
                    PlaceType = "Government Office",
                    Description = "Regional public administration complex with street level entrance, elevator access, and disability services counter on level 1.",
                    Address = "Agrabad Commercial Area",
                    City = "Chittagong",
                    District = "Chittagong",
                    Latitude = 22.3276,
                    Longitude = 91.8123,
                    WheelchairRamp = false,
                    Elevator = true,
                    AccessibleToilet = false,
                    AccessibleParking = true,
                    AccessibilityScore = 60,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

          
            modelBuilder.Entity<CommunityPost>()
                .HasIndex(p => p.CreatedAt);

            modelBuilder.Entity<CommunityComment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.CommunityPostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunityPostLike>()
                .HasIndex(l => new { l.CommunityPostId, l.UserId, l.UserRole })
                .IsUnique();

            modelBuilder.Entity<CommunityPostLike>()
                .HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.CommunityPostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunityReport>()
                .HasOne(r => r.Post)
                .WithMany(p => p.Reports)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunityReport>()
                .HasOne(r => r.Comment)
                .WithMany(c => c.Reports)
                .HasForeignKey(r => r.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunityReport>()
                .HasIndex(r => r.Status);

            
            modelBuilder.Entity<Admin>().HasData(
                new Admin
                {
                    Id = 1,
                    Email = "admin@abilityconnect.com",
                    PasswordHash = "$2a$11$5Y8qZ5Z5Z5Z5Z5Z5Z5Z5ZuY8qZ5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5",
                    FullName = "Administrator",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}