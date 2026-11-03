using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        string[] roles = [AppRoles.Admin, AppRoles.Manager, AppRoles.SalesExecutive];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Users
        ApplicationUser? adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.com");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@acxiomcrm.com",
                Email = "admin@acxiomcrm.com",
                FullName = "Admin User",
                RoleName = AppRoles.Admin,
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
            }
        }

        ApplicationUser? managerUser = await userManager.FindByEmailAsync("manager@acxiomcrm.com");
        if (managerUser == null)
        {
            managerUser = new ApplicationUser
            {
                UserName = "manager@acxiomcrm.com",
                Email = "manager@acxiomcrm.com",
                FullName = "Sales Manager",
                RoleName = AppRoles.Manager,
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(managerUser, "Manager@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, AppRoles.Manager);
            }
        }

        ApplicationUser? salesUser = await userManager.FindByEmailAsync("sales@acxiomcrm.com");
        if (salesUser == null)
        {
            salesUser = new ApplicationUser
            {
                UserName = "sales@acxiomcrm.com",
                Email = "sales@acxiomcrm.com",
                FullName = "Alex Morgan",
                RoleName = AppRoles.SalesExecutive,
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(salesUser, "Sales@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(salesUser, AppRoles.SalesExecutive);
            }
        }

        // 3. Seed Customers if empty
        if (!context.Customers.Any())
        {
            var customer1 = new Customer
            {
                CustomerCode = "CUST-1001",
                CustomerName = "Nexus Tech Solutions",
                Email = "contact@nexustech.io",
                Phone = "9876543210",
                CompanyName = "Nexus Enterprises",
                Address = "104 Tech Boulevard, Sector 5",
                City = "Bengaluru",
                State = "Karnataka",
                Status = "Active",
                CreatedDate = DateTime.UtcNow.AddDays(-20),
                CreatedBy = adminUser.Email,
                OwnerId = salesUser.Id
            };

            var customer2 = new Customer
            {
                CustomerCode = "CUST-1002",
                CustomerName = "Apex Global Logistics",
                Email = "ops@apexlogistics.com",
                Phone = "9876543211",
                CompanyName = "Apex Corp",
                Address = "55 Harbour View Road",
                City = "Mumbai",
                State = "Maharashtra",
                Status = "Active",
                CreatedDate = DateTime.UtcNow.AddDays(-15),
                CreatedBy = managerUser.Email,
                OwnerId = salesUser.Id
            };

            var customer3 = new Customer
            {
                CustomerCode = "CUST-1003",
                CustomerName = "Quantum Healthcare Systems",
                Email = "procure@quantumhealth.org",
                Phone = "9876543212",
                CompanyName = "Quantum Health Ltd",
                Address = "12 MediCity Complex",
                City = "Hyderabad",
                State = "Telangana",
                Status = "Active",
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                CreatedBy = adminUser.Email,
                OwnerId = managerUser.Id
            };

            context.Customers.AddRange(customer1, customer2, customer3);
            await context.SaveChangesAsync();

            // 4. Seed Leads
            var lead1 = new Lead
            {
                LeadCode = "LEAD-1001",
                LeadName = "David Miller",
                Email = "david.miller@finserve.net",
                Phone = "9876543220",
                CompanyName = "FinServe Capital",
                Source = "Website",
                Status = "Qualified",
                Priority = "High",
                ExpectedValue = 75000m,
                CreatedDate = DateTime.UtcNow.AddDays(-8),
                AssignedTo = salesUser.Id,
                Notes = "Interested in CRM enterprise licensing with 50+ seats."
            };

            var lead2 = new Lead
            {
                LeadCode = "LEAD-1002",
                LeadName = "Sophia Chen",
                Email = "sophia@solarenergy.org",
                Phone = "9876543221",
                CompanyName = "BrightSolar Innovations",
                Source = "Referral",
                Status = "Contacted",
                Priority = "Medium",
                ExpectedValue = 42000m,
                CreatedDate = DateTime.UtcNow.AddDays(-5),
                AssignedTo = salesUser.Id,
                Notes = "Initial demo scheduled for upcoming week."
            };

            var lead3 = new Lead
            {
                LeadCode = "LEAD-1003",
                LeadName = "Marcus Vance",
                Email = "marcus@cloudinfra.co",
                Phone = "9876543222",
                CompanyName = "CloudInfra Systems",
                Source = "Advertisement",
                Status = "New",
                Priority = "Medium",
                ExpectedValue = 30000m,
                CreatedDate = DateTime.UtcNow.AddDays(-2),
                AssignedTo = salesUser.Id,
                Notes = "Submitted contact form on landing page."
            };

            context.Leads.AddRange(lead1, lead2, lead3);
            await context.SaveChangesAsync();

            // 5. Seed Opportunities
            var opp1 = new Opportunity
            {
                OpportunityName = "Nexus Cloud Integration Project",
                CustomerId = customer1.CustomerId,
                Amount = 120000m,
                Stage = "Proposal",
                Probability = 60,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(25),
                Status = "Open",
                CreatedDate = DateTime.UtcNow.AddDays(-12),
                AssignedTo = salesUser.Id,
                Notes = "RFP proposal submitted, reviewing security compliance."
            };

            var opp2 = new Opportunity
            {
                OpportunityName = "Apex Fleet Tracking Module",
                CustomerId = customer2.CustomerId,
                Amount = 85000m,
                Stage = "Negotiation",
                Probability = 80,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(14),
                Status = "Open",
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                AssignedTo = salesUser.Id,
                Notes = "Final price negotiation ongoing with CFO."
            };

            var opp3 = new Opportunity
            {
                OpportunityName = "Quantum E-Health Portal",
                CustomerId = customer3.CustomerId,
                Amount = 190000m,
                Stage = "Won",
                Probability = 100,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(-2),
                Status = "Won",
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                AssignedTo = managerUser.Id,
                Notes = "Contract signed and onboarded successfully."
            };

            context.Opportunities.AddRange(opp1, opp2, opp3);
            await context.SaveChangesAsync();

            // 6. Seed FollowUps
            var fu1 = new FollowUp
            {
                CustomerId = customer1.CustomerId,
                FollowUpDate = DateTime.UtcNow.AddDays(2),
                FollowUpType = "Call",
                Subject = "Discuss revised proposal SLA terms",
                Remarks = "Call CTO regarding data residency SLA.",
                Status = "Planned",
                AssignedTo = salesUser.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-1)
            };

            var fu2 = new FollowUp
            {
                LeadId = lead1.LeadId,
                FollowUpDate = DateTime.UtcNow.AddDays(3),
                FollowUpType = "Meeting",
                Subject = "Technical Architecture Walkthrough",
                Remarks = "Online meeting with FinServe tech leads.",
                Status = "Planned",
                AssignedTo = salesUser.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-2)
            };

            var fu3 = new FollowUp
            {
                CustomerId = customer2.CustomerId,
                FollowUpDate = DateTime.UtcNow.AddDays(-1),
                FollowUpType = "Email",
                Subject = "Send updated discount structure",
                Remarks = "Pending email confirmation from client.",
                Status = "Missed",
                AssignedTo = salesUser.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-3)
            };

            context.FollowUps.AddRange(fu1, fu2, fu3);

            // 7. Seed Activities
            var act1 = new Activity
            {
                ActivityType = "Meeting",
                Subject = "Discovery call with Nexus Tech",
                Description = "Gathered initial requirement specs for cloud migration.",
                ActivityDate = DateTime.UtcNow.AddDays(-14),
                CustomerId = customer1.CustomerId,
                AssignedTo = salesUser.Id,
                Status = "Completed",
                CreatedDate = DateTime.UtcNow.AddDays(-14)
            };

            var act2 = new Activity
            {
                ActivityType = "Call",
                Subject = "Cold call follow-up with Sophia",
                Description = "Discussed CRM capabilities for solar panel distribution.",
                ActivityDate = DateTime.UtcNow.AddDays(-4),
                LeadId = lead2.LeadId,
                AssignedTo = salesUser.Id,
                Status = "Completed",
                CreatedDate = DateTime.UtcNow.AddDays(-4)
            };

            context.Activities.AddRange(act1, act2);

            // 8. Seed Initial Audit Logs
            context.AuditLogs.AddRange(
                new AuditLog
                {
                    UserId = adminUser.Id,
                    UserEmail = adminUser.Email,
                    Action = "System Initialized",
                    EntityName = "System",
                    RecordId = "0",
                    NewValue = "AcxiomCRM seeded with baseline roles, users, and demonstration data.",
                    CreatedDate = DateTime.UtcNow.AddDays(-20),
                    IpAddress = "127.0.0.1"
                },
                new AuditLog
                {
                    UserId = adminUser.Id,
                    UserEmail = adminUser.Email,
                    Action = "Create",
                    EntityName = "Customer",
                    RecordId = customer1.CustomerId.ToString(),
                    NewValue = "Created customer Nexus Tech Solutions (CUST-1001)",
                    CreatedDate = DateTime.UtcNow.AddDays(-20),
                    IpAddress = "127.0.0.1"
                }
            );

            await context.SaveChangesAsync();
        }
    }
}
