using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Chatbot;

namespace MV.ApplicationLayer.Services;

public class ChatbotService : IChatbotService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly bool _isConfigured;

    // ===== FAQ Database (keyword → response) =====
    private static readonly Dictionary<string, string> FaqDatabase = new(StringComparer.OrdinalIgnoreCase)
    {
        // Ordering
        { "order", "You can place an order directly on our website:\n1. Browse products → Add to cart\n2. Go to Cart → Checkout\n3. Fill in shipping info → Confirm\n\nWe support COD and SePay bank transfer." },
        { "buy", "To purchase, simply add items to your cart and proceed to checkout. We accept COD and SePay payments." },

        // Payment
        { "payment", "STEM Store supports 2 payment methods:\n• **COD** - Cash on Delivery\n• **SePay** - Bank transfer (auto-confirmed via QR scan)\n\nSePay orders expire after 10 minutes if unpaid." },
        { "pay", "We accept COD (Cash on Delivery) and SePay bank transfer. QR code is displayed on the order page after checkout." },

        // Shipping
        { "shipping", "Shipping fee: 30,000₫ per order (nationwide).\nDelivery time:\n• HCMC inner city: 1-2 days\n• Suburban areas: 3-5 days\n• Northern/Central regions: 5-7 days" },
        { "delivery", "Shipping fee: 30,000₫/order. Nationwide delivery available. Track your order in 'My Orders'." },
        { "ship", "Shipping: 30,000₫/order. Nationwide delivery, HCMC 1-2 days." },

        // Warranty
        { "warranty", "Warranty policies at STEM Store:\n• **Standard** - 12 months (manufacturer defects)\n• **Extended** - 24 months (comprehensive)\n• **Premium** - 36 months (30-day replacement, 24/7 support)\n\nSubmit warranty claims through your account dashboard." },
        { "guarantee", "All products include manufacturer warranty. Check your warranty status in 'My Warranties' section." },

        // Coupons
        { "coupon", "Enter coupon codes at checkout. Some active codes:\n• WELCOME2025 - 10% off (orders from 500K₫)\n• STUDENT15 - 15% off for students (orders from 300K₫)\n• FREESHIP - Free shipping" },
        { "discount", "Check our Promotions page for active coupons. Enter code at checkout to apply discount." },
        { "promo", "We regularly offer discounts and promotions. Enter coupon codes during checkout." },

        // Account
        { "register", "To create an account:\n1. Click 'Register' in the top right\n2. Fill in: Username, Email, Password\n3. Confirm registration\n\nAfter registering, you can place orders and track shipments." },
        { "login", "Click 'Login' → Enter Email and Password. If you forgot your password, contact admin for a reset." },
        { "sign up", "Click 'Register' to create a new account with your email. It only takes a minute!" },

        // Products
        { "arduino", "Arduino is the most popular microcontroller platform for STEM education. We carry:\n• Arduino Uno R3 - 350,000₫\n• Arduino Mega 2560 - 520,000₫\n• Arduino Nano V3 - 180,000₫\n• Arduino Starter Kit - 1,250,000₫\n\nBrowse our Microcontrollers category for more." },
        { "raspberry", "Raspberry Pi - powerful single-board computers:\n• Raspberry Pi 4B 4GB - 1,450,000₫\n• Raspberry Pi 4B 8GB - 1,850,000₫\n• Raspberry Pi Pico - 85,000₫\n• Complete Kit - 2,100,000₫" },
        { "esp32", "ESP32 - Affordable WiFi + Bluetooth modules:\n• ESP32 DevKit - 120,000₫\n• ESP32-CAM (with camera) - 150,000₫\n• ESP8266 NodeMCU - 75,000₫" },
        { "sensor", "We carry various sensors:\n• DHT22 (temperature/humidity) - 95,000₫\n• HC-SR04 (ultrasonic distance) - 45,000₫\n• PIR HC-SR501 (motion) - 35,000₫\n• MQ-2 (gas/smoke) - 55,000₫\n• BMP280 (barometric pressure) - 65,000₫\n\nOr get the Sensor Kit 37-in-1 for 680,000₫" },

        // Contact
        { "contact", "Contact STEM Store:\n📧 Email: support@stemstore.vn\n📞 Hotline: 0901-234-567\n🕐 Hours: 8:00 AM - 6:00 PM (Mon - Sat)" },
        { "support", "Need help? Reach us at:\n📧 support@stemstore.vn\n📞 0901-234-567\nOr use this chatbot for quick answers!" },

        // Returns
        { "return", "Return policy:\n• Returns accepted within 7 days for manufacturer defects\n• Products must be in original packaging with all accessories\n• Call our hotline 0901-234-567 for return instructions" },
        { "refund", "Refunds are processed within 7-14 business days after return confirmation. Refund via original payment method." },

        // Greetings
        { "hello", "👋 Hello! I'm STEM Bot, your AI assistant. How can I help you today? Ask about products, shipping, warranty, payment, or orders!" },
        { "hi", "👋 Hi there! I'm here to help you with anything STEM Store related. What would you like to know?" },
        { "thanks", "😊 You're welcome! Is there anything else I can help with?" },
    };

    // ===== System Prompt =====
    private const string SystemPrompt = @"You are the AI assistant for STEM Store — an e-commerce shop specializing in STEM products (Science, Technology, Engineering, Mathematics) including microcontrollers (Arduino, Raspberry Pi, ESP32), sensors, modules, learning kits, and electronic accessories.

Your role:
- Recommend STEM products suitable for the customer's needs
- Provide basic technical guidance on using components
- Answer questions about ordering, payment, and warranty
- Suggest suitable combos/kits for specific projects

Rules:
1. Reply concisely, clearly, and in a friendly manner
2. If a question is outside the STEM/electronics scope, politely decline and redirect to product topics
3. Always reply in English
4. Recommend specific products when possible (name, reference price)
5. Do not fabricate information — if unsure, say so and suggest contacting support";

    public ChatbotService(IConfiguration configuration)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        _apiKey = configuration["Groq:ApiKey"];
        _model = configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";

        if (string.IsNullOrEmpty(_apiKey))
        {
            _isConfigured = false;
            Console.WriteLine("[WARNING] Groq API key is missing. AI chatbot will only use FAQ responses.");
        }
        else
        {
            _isConfigured = true;
        }
    }

    public async Task<ChatbotResponse> AskAsync(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return new ChatbotResponse
            {
                Answer = "Please enter a question.",
                Source = "error"
            };
        }

        // Step 1: FAQ Match
        var faqAnswer = SearchFaq(question);
        if (faqAnswer != null)
        {
            return new ChatbotResponse
            {
                Answer = faqAnswer,
                Source = "faq"
            };
        }

        // Step 2: Groq AI Fallback
        if (!_isConfigured)
        {
            return new ChatbotResponse
            {
                Answer = "Sorry, the AI assistant is not configured yet. Please contact our hotline 0901-234-567 for support.",
                Source = "fallback"
            };
        }

        try
        {
            return await CallGroqApiAsync(question);
        }
        catch (TaskCanceledException)
        {
            return new ChatbotResponse
            {
                Answer = "Sorry, the AI system is busy. Please try again later or call our hotline 0901-234-567.",
                Source = "error"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Chatbot ERROR] {ex.Message}");
            return new ChatbotResponse
            {
                Answer = "Sorry, an error occurred. Please try again later.",
                Source = "error"
            };
        }
    }

    private string? SearchFaq(string question)
    {
        var lowerQ = question.ToLowerInvariant();
        foreach (var kvp in FaqDatabase)
        {
            if (lowerQ.Contains(kvp.Key.ToLowerInvariant()))
                return kvp.Value;
        }
        return null;
    }

    private async Task<ChatbotResponse> CallGroqApiAsync(string question)
    {
        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = question }
            },
            temperature = 0.7,
            max_tokens = 1024
        };

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[Groq API Error] {response.StatusCode}: {responseBody}");
            return new ChatbotResponse
            {
                Answer = "Sorry, the AI assistant is temporarily unavailable. Please try again later.",
                Source = "error"
            };
        }

        // Parse Groq response (OpenAI-compatible format)
        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return new ChatbotResponse
        {
            Answer = content ?? "No response from AI.",
            Source = "ai"
        };
    }
}
