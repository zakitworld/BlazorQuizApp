using BlazorQuizApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BlazorQuizApp.Services
{
    public static class DbInitializer
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration, ILogger logger)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "User", "Pro" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    logger.LogInformation("Creating role: {Role}", role);
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = configuration["AppSettings:AdminEmail"];
            var adminPassword = configuration["AppSettings:AdminPassword"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogInformation("Admin credentials are not configured; skipping admin user creation.");
                return;
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                logger.LogInformation("Creating admin user: {Email}", adminEmail);
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Administrator"
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    logger.LogInformation("Admin user created and assigned to Admin role.");
                }
                else
                {
                    logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        public static async Task SeedDefaultCategoriesAsync(IServiceProvider serviceProvider, ILogger logger)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var existingCategories = await db.QuizCategories
                    .Select(c => c.Name)
                    .ToListAsync();

                var existingSet = new HashSet<string>(existingCategories, StringComparer.OrdinalIgnoreCase);
                var toAdd = new List<QuizCategory>();

                foreach (var (name, sortOrder) in QuizCategorySections.DefaultCategories)
                {
                    if (!existingSet.Contains(name))
                    {
                        toAdd.Add(new QuizCategory
                        {
                            Name = name,
                            SortOrder = sortOrder,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (toAdd.Count > 0)
                {
                    logger.LogInformation("Seeding {Count} default quiz categories...", toAdd.Count);
                    await db.QuizCategories.AddRangeAsync(toAdd);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to seed default categories (may occur before migrations are applied)");
            }
        }

        public static async Task SeedSampleQuizResultsAsync(IServiceProvider serviceProvider, ILogger logger)
        {
            logger.LogInformation("Checking sample quiz results...");
            await Task.CompletedTask;
        }

        public static async Task SeedSampleQuestionsAsync(IServiceProvider serviceProvider, ILogger logger)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var sampleQuestions = GetStarterQuestions();
                var seededCount = 0;

                foreach (var sample in sampleQuestions)
                {
                    var exists = await db.QuizQuestions.AnyAsync(q =>
                        q.Category == sample.Category && q.QuestionText == sample.QuestionText);

                    if (!exists)
                    {
                        var question = new QuizQuestion
                        {
                            QuestionText = sample.QuestionText,
                            Category = sample.Category,
                            Difficulty = sample.Difficulty,
                            Type = QuestionType.MultipleChoice,
                            Explanation = sample.Explanation,
                            CreatedAt = DateTime.UtcNow,
                            Options = sample.Options.Select(o => new QuizOption { Text = o }).ToList()
                        };

                        db.QuizQuestions.Add(question);
                        await db.SaveChangesAsync();

                        if (sample.CorrectIndex >= 0 && sample.CorrectIndex < question.Options.Count)
                        {
                            question.CorrectOptionId = question.Options[sample.CorrectIndex].Id;
                            question.CorrectOptionIdsJson = JsonSerializer.Serialize(new[] { question.CorrectOptionId });
                            await db.SaveChangesAsync();
                        }

                        seededCount++;
                    }
                }

                if (seededCount > 0)
                {
                    logger.LogInformation("Successfully seeded {Count} starter quiz questions.", seededCount);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to seed sample questions (may occur before migrations are applied)");
            }
        }

        private record StarterQuestion(
            string Category,
            string Difficulty,
            string QuestionText,
            string[] Options,
            int CorrectIndex,
            string Explanation);

        private static List<StarterQuestion> GetStarterQuestions() =>
        [
            // ── Computing ──
            new StarterQuestion(
                "Computing",
                "Easy",
                "Which component of a computer system is considered volatile, meaning all stored data is lost when the power is turned off?",
                ["Read-Only Memory (ROM)", "Random Access Memory (RAM)", "Solid State Drive (SSD)", "Hard Disk Drive (HDD)"],
                1,
                "Random Access Memory (RAM) is high-speed volatile primary memory used by the CPU for active processes; its content is cleared once power ceases."
            ),
            new StarterQuestion(
                "Computing",
                "Medium",
                "What is the term for a step-by-step, finite sequence of unambiguous instructions designed to perform a specific task or solve a computational problem?",
                ["Algorithm", "Flowchart", "Pseudocode", "Heuristic"],
                0,
                "An algorithm is a formal, finite sequence of well-defined steps or instructions for calculation, data processing, and automated reasoning."
            ),
            new StarterQuestion(
                "Computing",
                "Medium",
                "What is the primary role of the Arithmetic Logic Unit (ALU) within the Central Processing Unit (CPU)?",
                [
                    "Managing network bandwidth and latency",
                    "Performing arithmetic operations (addition, subtraction) and logical decisions (AND, OR, NOT)",
                    "Storing operating system boot files permanently",
                    "Regulating electric voltage supplied to system fans"
                ],
                1,
                "The ALU is a digital circuit inside the processor that executes all mathematical computations and Boolean logical evaluations."
            ),

            // ── ICT ──
            new StarterQuestion(
                "ICT",
                "Easy",
                "In Information and Communication Technology, what is the fundamental difference between 'data' and 'information'?",
                [
                    "Data is always encrypted, while information is plain text",
                    "Data is raw, unorganized facts, while information is structured, processed, and meaningful",
                    "Data exists only in the cloud, while information is stored on physical paper",
                    "Data is measured in hertz, while information is measured in volts"
                ],
                1,
                "Data consists of raw, unprocessed figures and symbols; once contextualized, processed, and given meaning, it becomes information."
            ),
            new StarterQuestion(
                "ICT",
                "Medium",
                "Which technology allows users to access shared computing resources (servers, storage, databases, and software) on-demand over the internet rather than hosting them locally?",
                ["Cloud Computing", "Peer-to-Peer Tunneling", "Bluetooth Mesh", "Direct Dial-Up"],
                0,
                "Cloud computing provides on-demand delivery of IT resources and applications over the internet with pay-as-you-go pricing."
            ),
            new StarterQuestion(
                "ICT",
                "Easy",
                "What is the recommended practice for creating strong, secure passwords for online digital services?",
                [
                    "Using your date of birth or name so you don't forget it",
                    "Using a combination of uppercase letters, lowercase letters, numbers, and symbols, of adequate length",
                    "Using the exact same password across every website and social platform",
                    "Writing your password on a sticky note attached to your monitor"
                ],
                1,
                "Strong passwords combine varying character types (uppercase, lowercase, numbers, symbols) and sufficient length, avoiding personal or dictionary words."
            ),

            // ── Networking ──
            new StarterQuestion(
                "Networking",
                "Medium",
                "Which layer of the OSI model is primarily responsible for end-to-end flow control and reliable delivery via protocols like TCP?",
                ["Network Layer (Layer 3)", "Transport Layer (Layer 4)", "Data Link Layer (Layer 2)", "Session Layer (Layer 5)"],
                1,
                "Layer 4 (Transport Layer) ensures reliable data transfer between end systems using protocols like TCP, handling segmentation, flow control, and error correction."
            ),
            new StarterQuestion(
                "Networking",
                "Easy",
                "What is the default subnet mask for a standard Class C IPv4 network (/24)?",
                ["255.0.0.0", "255.255.0.0", "255.255.255.0", "255.255.255.255"],
                2,
                "A /24 CIDR prefix represents 24 network bits, which corresponds to the decimal subnet mask 255.255.255.0."
            ),
            new StarterQuestion(
                "Networking",
                "Hard",
                "Which protocol is used by routers on the public internet to exchange routing and reachability information between Autonomous Systems (AS)?",
                ["OSPF (Open Shortest Path First)", "RIPv2 (Routing Information Protocol)", "BGP (Border Gateway Protocol)", "EIGRP"],
                2,
                "BGP (Border Gateway Protocol) is the de-facto exterior gateway protocol designed to route traffic between distinct autonomous systems across the internet."
            ),

            // ── Cybersecurity ──
            new StarterQuestion(
                "Cybersecurity",
                "Easy",
                "What are the three core principles of information security collectively known as the CIA Triad?",
                [
                    "Control, Identification, Authentication",
                    "Confidentiality, Integrity, Availability",
                    "Compliance, Inspection, Audit",
                    "Cryptography, Isolation, Access"
                ],
                1,
                "The CIA Triad stands for Confidentiality, Integrity, and Availability—the fundamental pillars of information security."
            ),
            new StarterQuestion(
                "Cybersecurity",
                "Medium",
                "Which cryptographic concept enables two parties to securely communicate using a public key for encryption and a private key for decryption?",
                ["Symmetric Encryption", "Asymmetric (Public Key) Encryption", "Hashing", "Steganography"],
                1,
                "Asymmetric encryption uses mathematically linked key pairs: a public key available to anyone for encryption and a private key kept secret for decryption."
            ),
            new StarterQuestion(
                "Cybersecurity",
                "Hard",
                "What principle dictates that users, processes, and systems should only operate with the bare minimum permissions necessary to accomplish their tasks?",
                ["Principle of Least Privilege", "Separation of Duties", "Defense in Depth", "Implicit Deny"],
                0,
                "The Principle of Least Privilege (PoLP) minimizes the attack surface by restricting access permissions to only what is strictly required for the function."
            ),

            // ── Ethical Hacking ──
            new StarterQuestion(
                "Ethical Hacking",
                "Medium",
                "In ethical hacking and penetration testing methodologies, what is the initial phase where an analyst gathers intelligence about the target without direct exploitation?",
                ["Reconnaissance (Footprinting)", "Privilege Escalation", "Covering Tracks", "Exploitation"],
                0,
                "Reconnaissance (Footprinting) is the first phase, involving passive and active collection of target information (DNS records, employee info, open ports, tech stack)."
            ),
            new StarterQuestion(
                "Ethical Hacking",
                "Medium",
                "Which command-line tool is most commonly utilized for network discovery, port scanning, and OS detection during security evaluations?",
                ["Wireshark", "Nmap", "John the Ripper", "Hashcat"],
                1,
                "Nmap (Network Mapper) is the industry standard utility for network discovery, port scanning, service versioning, and vulnerability scanning."
            ),
            new StarterQuestion(
                "Ethical Hacking",
                "Hard",
                "An attacker injects malicious SQL statements into an unescaped input parameter to bypass authentication. What is the most effective defensive remediation?",
                [
                    "Client-side JavaScript input validation",
                    "Parameterized queries / Prepared statements",
                    "Increasing the database password length",
                    "Encrypting the database connection string"
                ],
                1,
                "Parameterized queries (Prepared Statements) ensure user input is treated strictly as data rather than executable code, eliminating SQL Injection vulnerabilities."
            ),

            // ── Penetration Testing ──
            new StarterQuestion(
                "Penetration Testing",
                "Medium",
                "What type of penetration testing is conducted when the tester has zero prior knowledge of the internal network architecture or source code?",
                ["White Box Testing", "Grey Box Testing", "Black Box Testing", "Red Box Testing"],
                2,
                "Black Box testing simulates an external attacker's perspective without any prior insider documentation, network schematics, or credentials."
            ),
            new StarterQuestion(
                "Penetration Testing",
                "Hard",
                "After gaining initial low-privilege shell access on a target machine, what technique is used to elevate to administrative or root privileges?",
                ["Privilege Escalation", "Lateral Movement", "Port Forwarding", "Brute Force"],
                0,
                "Privilege Escalation refers to exploiting misconfigurations, kernel bugs, or weak permissions to elevate from a standard user to root/SYSTEM."
            ),

            // ── BECE (Basic Education Certificate Examination) ──
            new StarterQuestion(
                "BECE",
                "Easy",
                "In Integrated Science, what is the green pigment in plant leaves responsible for absorbing sunlight for photosynthesis?",
                ["Carotene", "Chlorophyll", "Anthocyanin", "Xanthophyll"],
                1,
                "Chlorophyll is the green pigment located within chloroplasts that absorbs light energy necessary to drive the process of photosynthesis."
            ),
            new StarterQuestion(
                "BECE",
                "Medium",
                "In Mathematics: If 3x + 7 = 22, what is the value of x?",
                ["3", "5", "7", "15"],
                1,
                "Subtracting 7 from both sides gives 3x = 15. Dividing both sides by 3 gives x = 5."
            ),
            new StarterQuestion(
                "BECE",
                "Easy",
                "In Social Studies: Which river was dammed at Akosombo to generate hydroelectric power in Ghana?",
                ["River Pra", "River Volta", "River Ankobra", "River Densu"],
                1,
                "The Akosombo Dam was constructed on the River Volta between 1961 and 1965 to create Lake Volta and generate electricity for Ghana."
            ),

            // ── WASSCE (West African Senior School Certificate Examination) ──
            new StarterQuestion(
                "WASSCE",
                "Medium",
                "In Core Mathematics: What are the roots of the quadratic equation x² - 5x + 6 = 0?",
                ["x = 2 and x = 3", "x = -2 and x = -3", "x = 1 and x = 6", "x = -1 and x = 6"],
                0,
                "Factoring x² - 5x + 6 gives (x - 2)(x - 3) = 0. Therefore, the roots are x = 2 and x = 3."
            ),
            new StarterQuestion(
                "WASSCE",
                "Hard",
                "In Integrated Science: What is the oxidation state of sulfur (S) in sulfuric acid (H₂SO₄)?",
                ["+2", "+4", "+6", "-2"],
                2,
                "In H₂SO₄, hydrogen has +1 (total +2) and oxygen has -2 (total -8). For a neutral molecule, 2 + S + (-8) = 0, which yields S = +6."
            ),
            new StarterQuestion(
                "WASSCE",
                "Medium",
                "In Biology: Which cellular organelle is recognized as the powerhouse of the cell due to ATP synthesis during cellular respiration?",
                ["Ribosome", "Endoplasmic Reticulum", "Mitochondrion", "Golgi apparatus"],
                2,
                "The mitochondrion produces the majority of cellular adenosine triphosphate (ATP) through the citric acid cycle and oxidative phosphorylation."
            ),

            // ── TEACHERS PROMOTION ──
            new StarterQuestion(
                "TEACHERS PROMOTION",
                "Medium",
                "Under the Ghana Education Service (GES) framework, which school-level committee brings together headteachers, teachers, parents, and community leaders to support school governance?",
                ["School Management Committee (SMC)", "Disciplinary Board", "Student Representative Council", "Textbook Selection Panel"],
                0,
                "The School Management Committee (SMC) is the community-based school governance body representing the community and collaborating with GES to manage basic schools."
            ),
            new StarterQuestion(
                "TEACHERS PROMOTION",
                "Hard",
                "What is the primary pedagogical purpose of Formative Assessment (Assessment FOR Learning) in modern classroom practice?",
                [
                    "To assign end-of-term letter grades to students",
                    "To diagnose student learning gaps in real-time and adapt ongoing teaching strategies",
                    "To rank schools in national performance tables",
                    "To determine teacher promotion eligibility based on pass rates"
                ],
                1,
                "Formative assessment provides timely feedback during the instruction process, enabling educators to adjust pedagogy and learners to improve understanding before summative tests."
            ),
            new StarterQuestion(
                "TEACHERS PROMOTION",
                "Medium",
                "Which Continuous Professional Development (CPD) structure is mandated by the National Teaching Council for teachers to renew their teaching licenses?",
                ["Accumulating verified CPD points via workshops and PLC meetings", "Taking an annual written examination", "Submitting paper lesson notebooks", "Teaching for at least 15 continuous years without absence"],
                0,
                "The National Teaching Council (NTC) framework requires licensed teachers to accumulate designated CPD points through accredited training, webinars, and Professional Learning Community (PLC) sessions for license renewal."
            ),

            // ── NTC Licensure Exams (GTLE) ──
            new StarterQuestion(
                "NTC Licensure Exams",
                "Medium",
                "Under the National Teachers' Standards (NTS) in Ghana, what are the three overarching professional pillars expected of every teacher?",
                [
                    "Professional Values and Attitudes, Professional Knowledge, and Professional Practice",
                    "Administration, Finance, and Community Mobilization",
                    "Curriculum Design, Examination Setting, and Grading",
                    "Research, Publication, and Institutional Governance"
                ],
                0,
                "The Ghana National Teachers' Standards (NTS) define professional teaching practice through three pillars: Professional Values & Attitudes, Professional Knowledge, and Professional Practice."
            ),
            new StarterQuestion(
                "NTC Licensure Exams",
                "Easy",
                "In the Literacy section of the Ghana Teacher Licensure Examination, what does 'phonemic awareness' refer to?",
                [
                    "The ability to hear, identify, and manipulate individual sounds in spoken words",
                    "The speed at which a person can read printed text silently",
                    "The understanding of figurative language and idioms",
                    "The study of sentence syntax and grammatical clauses"
                ],
                0,
                "Phonemic awareness is the foundational auditory ability to distinguish and manipulate individual sounds (phonemes) in spoken language."
            ),
            new StarterQuestion(
                "NTC Licensure Exams",
                "Medium",
                "According to the Code of Professional Conduct for Teachers, what is the required response when a teacher identifies a child showing consistent signs of abuse or neglect?",
                [
                    "Ignore it to preserve family privacy",
                    "Document and report following school child protection and safeguarding procedures",
                    "Confront the parents publicly during assembly",
                    "Wait until the end of the academic year before taking action"
                ],
                1,
                "Teachers have a duty of care to adhere to statutory safeguarding policies, reporting signs of harm or neglect promptly through designated child protection channels."
            )
        ];
    }
}
