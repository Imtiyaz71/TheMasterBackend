using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TheMaster.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IDbConnectionFactory>>();

            try
            {
                logger.LogInformation("Verifying and auto-initializing database tables using Dapper...");

                using var db = connectionFactory.CreateConnection();

                // 1. Ensure AdminLogin table exists
                var createAdminLoginSql = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AdminLogin' AND type = 'U')
                    BEGIN
                        CREATE TABLE AdminLogin (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            FullName NVARCHAR(150) NOT NULL DEFAULT 'Super Admin',
                            Email NVARCHAR(150) NOT NULL UNIQUE,
                            Password NVARCHAR(255) NOT NULL,
                            PasswordHash NVARCHAR(255) NULL,
                            Role NVARCHAR(50) NOT NULL DEFAULT 'Admin',
                            IsActive BIT NOT NULL DEFAULT 1,
                            CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                        );
                    END";
                await db.ExecuteAsync(createAdminLoginSql);

                // Seed default admin in AdminLogin
                var defaultAdminEmail = "abc@bd.com";
                var defaultAdminCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AdminLogin WHERE Email = @Email", new { Email = defaultAdminEmail });
                if (defaultAdminCount == 0)
                {
                    var hash123 = BCrypt.Net.BCrypt.HashPassword("123");
                    var insertAdminLoginSql = @"
                        INSERT INTO AdminLogin (FullName, Email, Password, PasswordHash, Role, IsActive, CreatedAt)
                        VALUES ('Super Admin', @Email, '123', @PasswordHash, 'Admin', 1, SYSUTCDATETIME());";
                    await db.ExecuteAsync(insertAdminLoginSql, new { Email = defaultAdminEmail, PasswordHash = hash123 });
                    logger.LogInformation("Default AdminLogin seeded: {Email} (Password: 123)", defaultAdminEmail);
                }

                // 2. Check if Users table exists; if not, execute idempotent script
                var tableCheckSql = "SELECT COUNT(*) FROM sys.tables WHERE name = 'Users' AND type = 'U';";
                var tableExists = await db.ExecuteScalarAsync<int>(tableCheckSql) > 0;

                if (!tableExists)
                {
                    logger.LogInformation("Database tables not found. Executing idempotent table schema creation script...");
                    
                    var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Database", "Scripts", "init_tables.sql");
                    if (!File.Exists(scriptPath))
                    {
                        scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "Scripts", "init_tables.sql");
                    }

                    if (File.Exists(scriptPath))
                    {
                        var fullSql = await File.ReadAllTextAsync(scriptPath);
                        var batches = fullSql.Split(new[] { "GO\r\n", "GO\n", "go\r\n", "go\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var batch in batches)
                        {
                            var sql = batch.Trim();
                            if (!string.IsNullOrWhiteSpace(sql) && !sql.StartsWith("USE [", StringComparison.OrdinalIgnoreCase))
                            {
                                await db.ExecuteAsync(sql);
                            }
                        }
                    }
                }

                // 3. Seed Super Admin in Users table if not present
                var adminEmail = "admin@themaster.com";
                var adminCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users WHERE Email = @Email", new { Email = adminEmail });
                if (adminCount == 0)
                {
                    var hash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                    var insertAdminSql = @"
                        INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, Role, IsActive, CreatedAt)
                        VALUES ('The Master Admin', @Email, @PasswordHash, '01700000000', 'Admin', 1, SYSUTCDATETIME());";
                    
                    await db.ExecuteAsync(insertAdminSql, new { Email = adminEmail, PasswordHash = hash });
                    logger.LogInformation("Default Super Admin seeded: {Email} (Password: Admin@123)", adminEmail);
                }

                // Also ensure abc@bd.com exists in Users table with Admin role
                var abcUserCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users WHERE Email = @Email", new { Email = defaultAdminEmail });
                if (abcUserCount == 0)
                {
                    var hash123 = BCrypt.Net.BCrypt.HashPassword("123");
                    var insertAbcUserSql = @"
                        INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, Role, IsActive, CreatedAt)
                        VALUES ('Super Admin', @Email, @PasswordHash, '01700000000', 'Admin', 1, SYSUTCDATETIME());";
                    await db.ExecuteAsync(insertAbcUserSql, new { Email = defaultAdminEmail, PasswordHash = hash123 });
                    logger.LogInformation("Admin abc@bd.com synchronized into Users table.");
                }

                // 4. Seed Sample Course if empty
                var courseCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Courses;");
                if (courseCount == 0)
                {
                    var insertCourseSql = @"
                        INSERT INTO Courses (Title, Slug, Subject, Description, ThumbnailUrl, Price, DiscountPrice, ValidityDays, TotalVideos, DriveFolderLink, IsPublished, CreatedAt)
                        VALUES (
                            'ডিজিটাল মার্কেটিং ও সোশ্যাল মিডিয়া স্ট্র্যাটেজি মাস্টারক্লাস',
                            'digital-marketing-masterclass',
                            'Digital Marketing',
                            'ফেসবুক ও ইনস্টাগ্রাম অ্যাডস, গুগল অ্যাডস, সেলস ফানেল এবং লিড জেনারেশনের বাস্তবমুখী প্রফেশনাল ট্রেনিং।',
                            'https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=800&auto=format&fit=crop',
                            3500.00,
                            1999.00,
                            365,
                            42,
                            'https://drive.google.com/drive/folders/sample-digital-marketing-course',
                            1,
                            SYSUTCDATETIME()
                        );
                        SELECT CAST(SCOPE_IDENTITY() as int);";

                    var courseId = await db.ExecuteScalarAsync<int>(insertCourseSql);

                    var insertLessonSql = @"
                        INSERT INTO Lessons (CourseId, LessonNumber, Title, Description, DurationMinutes, DriveVideoLink, IsFreePreview, CreatedAt)
                        VALUES 
                        (@CourseId, 1, 'মডিউল ১: ডিজিটাল মার্কেটিং ফান্ডামেন্টালস ও অডিয়েন্স রিসার্চ', 'টার্গেট অডিয়েন্স ও কাস্টমার জার্নি ম্যাপিং', 40, 'https://drive.google.com/file/d/sample-video-1', 1, SYSUTCDATETIME()),
                        (@CourseId, 2, 'মডিউল ২: মেটা বিজনেস স্যুট ও হাই-কনভার্টিং অ্যাড ক্যাম্পেইন', 'বাজেটিং, আরওআই ও রিটার্গেটিং অ্যাড সেটআপ', 55, 'https://drive.google.com/file/d/sample-video-2', 0, SYSUTCDATETIME());";

                    await db.ExecuteAsync(insertLessonSql, new { CourseId = courseId });
                    logger.LogInformation("Sample Course & Lessons seeded with Dapper.");
                }

                // 5. Ensure Testimonials table exists and seed default data
                var createTestimonialsSql = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Testimonials' AND type = 'U')
                    BEGIN
                        CREATE TABLE Testimonials (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            StudentName NVARCHAR(150) NOT NULL,
                            Designation NVARCHAR(150) NOT NULL,
                            Speech NVARCHAR(MAX) NOT NULL,
                            AvatarUrl NVARCHAR(MAX) NULL,
                            Rating INT NOT NULL DEFAULT 5,
                            DisplayOrder INT NOT NULL DEFAULT 0,
                            IsActive BIT NOT NULL DEFAULT 1,
                            CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                            UpdatedAt DATETIME2 NULL
                        );
                    END";
                await db.ExecuteAsync(createTestimonialsSql);

                var testimonialCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Testimonials;");
                if (testimonialCount == 0)
                {
                    var seedTestimonialsSql = @"
                        INSERT INTO Testimonials (StudentName, Designation, Speech, AvatarUrl, Rating, DisplayOrder, IsActive, CreatedAt)
                        VALUES 
                        (N'তানভীর আহমেদ', N'ডিজিটাল মার্কেটিং এক্সপার্ট (আপওয়ার্ক টপ-রেটেড)', N'The Master-এর কোর্স ও মেটরিয়াল এত সাজানো ও আপডেট যে প্রথম মাসেই আপওয়ার্কে আমার প্রথম বড় ক্লায়েন্ট পাই। যেকোনো সমস্যায় প্র্যাকটিক্যাল গাইডলাইন পেয়েছি।', 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop', 5, 1, 1, SYSUTCDATETIME()),
                        (N'সাইফুল ইসলাম', N'গ্রাফিক ডিজাইনার ও ফ্রিল্যান্সার', N'বেসিক থেকে অ্যাডভান্সড পর্যন্ত এত সুন্দর করে বুঝানো হয়েছে যা কোনো সাধারণ কোর্সে পাওয়া যায় না। লাইফটাইম অ্যাক্সেসের সুবিধা আমাকে অনেক সাহায্য করেছে।', 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&auto=format&fit=crop', 5, 2, 1, SYSUTCDATETIME()),
                        (N'রাফিয়া সুলতানা', N'এসইও স্পেশালিস্ট (রিমোট জব)', N'লাইভ কেস স্টাডি ও মেটেরিয়ালগুলো চমৎকার। কোর্স শেষ করার সাথে সাথেই একটি ইন্টারন্যাশনাল এজেন্সিতে রিমোট এসইও প্রজেক্টে কাজ শুরু করি। আলহামদুলিল্লাহ!', 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150&auto=format&fit=crop', 5, 3, 1, SYSUTCDATETIME());";
                    await db.ExecuteAsync(seedTestimonialsSql);
                    logger.LogInformation("Sample Testimonials seeded with Dapper.");
                }

                logger.LogInformation("Database verification and initialization completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error initializing database with Dapper.");
            }
        }
    }
}
