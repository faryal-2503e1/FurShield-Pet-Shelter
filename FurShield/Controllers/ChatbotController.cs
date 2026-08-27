using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FurShield.Controllers
{
    public class ChatbotController : Controller
    {
        [HttpPost]
        public IActionResult Ask([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return Json(new { response = "Kripya apna sawal detail mein likhein." });
            }

            string userQuery = request.Prompt.ToLower();
            string botResponse = GetBotAnswer(userQuery);

            return Json(new { response = botResponse });
        }

        // Ordered list = intent-based FAQ engine. Each entry has a set of
        // keywords (English + Roman Urdu). First matching entry wins, so put
        // more specific topics before generic ones (e.g. "cancel appointment"
        // before plain "appointment").
        private static readonly List<(string[] Keywords, string Answer)> FaqRules = new()
        {
            // ---------- Greeting / Help ----------
            (new[] { "hello", "hi", "hey", "salam", "assalam", "menu", "help", "options" },
             "Hello! Main aapka **FurShield Pet Care Assistant** hoon. Aap mujhse in mein se kisi bhi topic par pooch sakte hain: " +
             "<br/>🐾 Pet Registration/Login &nbsp; 🐾 Pet Profile &nbsp; 🐾 Health Records &nbsp; 🐾 Vaccination " +
             "<br/>🐾 Vet Appointment &nbsp; 🐾 Feeding &nbsp; 🐾 Grooming &nbsp; 🐾 Products/Shopping " +
             "<br/>🐾 Pet Adoption &nbsp; 🐾 Shelter &nbsp; 🐾 Family Sharing &nbsp; 🐾 Notifications &nbsp; 🐾 Contact Us"),

            // ---------- Registration / Login ----------
            (new[] { "register", "registration", "sign up", "signup", "account bana", "naya account", "login", "log in", "sign in" },
             "📝 **Registration/Login:**<br/>Pet Owner, Veterinarian, aur Shelter — teeno apna account banwa sakte hain. Registration ke waqt name, contact number, email ID, aur address dena zaroori hai. Account bann jaye toh **Login** page se apne dashboard par ja sakte hain."),

            // ---------- Family sharing ----------
            (new[] { "family", "gharwal", "invite code", "share account", "shared account" },
             "👨‍👩‍👧 **Family Sharing:**<br/>Aap apna pet account family members ke saath share kar sakte hain. **Family** section mein ja kar naya family group create karein ya kisi ka **invite code** daal kar us family mein join karein."),

            // ---------- Pet Profile ----------
            (new[] { "profile", "pet add", "add pet", "naya pet", "pet detail", "breed", "gallery", "image gallery", "pet ki tasveer" },
             "🐕 **Pet Profile Management:**<br/>Aap apne pets ka naam, species, breed, age, aur medical history add/edit/delete kar sakte hain. Multiple pets ke liye tabbed interface milta hai, aur har pet ki image gallery bhi maintain kar sakte hain."),

            // ---------- Health Records ----------
            (new[] { "health record", "medical history", "timeline", "x-ray", "xray", "lab report", "insurance", "certificate", "document upload", "vet certificate" },
             "📋 **Health Records:**<br/>Pet ke vaccination dates, allergies, illness aur treatment history yahan record hoti hai — ek visual timeline ke saath. Aap vet certificates, X-rays, lab reports, aur insurance policy documents bhi upload/store/view kar sakte hain."),

            // ---------- Vaccination ----------
            (new[] { "vaccine", "vaccination", "injection", "teeka", "booster" },
             "💉 **Vaccination Schedule:**<br/>- Puppies/Kittens ko 6-8 weeks ki umar se primary vaccines lagwani chahiye.<br/>- Rabies vaccine ka annual booster zaruri hota hai.<br/>- Poori vaccination history aap **Health Records** section mein dekh sakte hain."),

            // ---------- Health emergency ----------
            (new[] { "fever", "bukhar", "sick", "bimar", "vomit", "ulti", "weak", "kamzori", "injured", "zakhmi" },
             "⚠️ **Health Alert:**<br/>Agar pet ko bukhar, ulti, ya weakness hai, toh use ghar par human medicines (jaise Panadol) mat dein. Turant **'Appointment Booking'** section se kisi Veterinarian se rabta karein!"),

            // ---------- Feeding ----------
            (new[] { "food", "diet", "khana", "feed", "feeding" },
             "🐶 **Pet Feeding Guide:**<br/>- Clean water hamesha accessible rakhein.<br/>- Chocolate, onion, garlic, aur grapes pets ke liye poisonous hote hain.<br/>- High-protein, age-appropriate commercial pet food dein.<br/>Aap **Care Tips** section mein feeding se related articles aur FAQs bhi dekh sakte hain."),

            // ---------- Grooming / Hygiene ----------
            (new[] { "bath", "shower", "hygiene", "cleaning", "grooming", "nehla" },
             "🧼 **Hygiene & Grooming Tips:**<br/>- Dogs ko mahine mein 1-2 baar mild pet shampoo se nehlayein.<br/>- Cats khud ko saaf rakhti hain, unhe regular brushing ki zaroorat hoti hai.<br/>Grooming products aap **Shop** section se bhi kharid sakte hain."),

            // ---------- Appointment: cancel/reschedule (specific, before generic) ----------
            (new[] { "cancel appointment", "reschedule", "change appointment", "appointment cancel", "slot change" },
             "🔁 **Reschedule/Cancel Appointment:**<br/>Apni booked appointment **Appointments** section mein dekh kar reschedule ya cancel kar sakte hain. Veterinarian bhi apni side se appointment approve ya reschedule kar sakta hai."),

            // ---------- Appointment booking (generic) ----------
            (new[] { "appointment", "book", "vet", "doctor", "consult" },
             "📅 **Appointment Booking:**<br/>Aap humari website par **'Book Appointment'** section mein ja kar nearest Veterinarian ke available time slots dekh kar booking kar sakte hain. App aapke pet ki condition/location ke hisaab se vets bhi suggest karta hai."),

            // ---------- Products / Shopping / Cart / Orders ----------
            (new[] { "product", "shop", "buy", "kharid", "cart", "order", "toy", "accessories", "checkout" },
             "🛒 **Pet Products:**<br/>Aap food, grooming items, accessories, aur health supplies **Shop** section mein browse kar sakte hain — category aur filters ke saath. Items ko **Cart** mein add/remove/modify kar ke order place kar sakte hain.<br/>Note: Payment gateway aur physical delivery is application ka hissa nahi hai."),

            // ---------- Adoption (owner side: browse & apply) ----------
            (new[] { "adopt", "adoption", "goad lena", "adoptable pet" },
             "🐾 **Pet Adoption:**<br/>Adoptable pets **Adopt** section mein species/breed ke hisaab se search kar sakte hain. Kisi pet mein interest ho toh **interest form** submit karein — shelter aapko response dega."),

            // ---------- Shelter features ----------
            (new[] { "shelter", "panagah", "list pet", "care status", "shelter animal" },
             "🏠 **Animal Shelter Features:**<br/>Shelters apna account register kar ke adoptable pets list kar sakte hain (images, age, breed, health status ke saath), unka feeding/grooming/medical care status update kar sakte hain, aur adopters ke interest forms par respond kar sakte hain."),

            // ---------- Veterinarian features ----------
            (new[] { "veterinarian", "vet profile", "specialization", "availability", "time slot", "treatment log" },
             "👩‍⚕️ **Veterinarian Features:**<br/>Vets apna profile (specialization, experience, available time slots) manage kar sakte hain, appointments approve/reschedule kar sakte hain, patient ki medical history dekh sakte hain, aur diagnosis, treatment, aur prescription notes log kar sakte hain."),

            // ---------- Notifications ----------
            (new[] { "notification", "alert", "reminder", "yaad dehani" },
             "🔔 **Notifications:**<br/>Vaccination due dates, appointment confirmations, aur naye product arrivals par aapko email/in-app notifications milti hain — **Notifications** section mein check kar sakte hain."),

            // ---------- Ratings / Feedback ----------
            (new[] { "rating", "review", "feedback", "rate" },
             "⭐ **Ratings & Feedback:**<br/>Aap veterinarians, shelters, ya products ko rate kar sakte hain aur comment/feedback bhi de sakte hain."),

            // ---------- About / Contact ----------
            (new[] { "about us", "about", "contact", "location", "address", "team" },
             "ℹ️ **About/Contact Us:**<br/>Hamari team aur portal ki details **About Us** page par milengi, aur **Contact Us** page par static contact info Google Maps location ke saath diya gaya hai."),
        };

        private string GetBotAnswer(string query)
        {
            foreach (var rule in FaqRules)
            {
                if (rule.Keywords.Any(k => query.Contains(k)))
                {
                    return rule.Answer;
                }
            }

            // Default fallback response — never leaves the user without direction
            return "Main aapka sawal poori tarah nahi samajh paya. Aap mujhse **Registration, Pet Profile, Health Records, Vaccination, Appointment, Feeding, Grooming, Products, Adoption, Shelter,** ya **Notifications** ke baare mein pooch sakte hain! (Type 'menu' for full list)";
        }
    }

    public class ChatRequest
    {
        public string Prompt { get; set; } = string.Empty;
    }
}