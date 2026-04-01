using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Chatbot;
using MV.DomainLayer.DTOs.RequestModels;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class ChatbotService : IChatbotService
{
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly bool _isConfigured;

    // ===== FAQ Database (keyword → response) =====
    private static readonly Dictionary<string, string> FaqDatabase = new(StringComparer.OrdinalIgnoreCase)
    {
        // Ordering
        { "order", "You can place an order directly on our app:\n1. Browse products → Add to cart\n2. Go to Cart → Checkout\n3. Fill in shipping info → Confirm\n\nWe support COD and SePay bank transfer." },
        { "buy", "To purchase, simply add items to your cart and proceed to checkout. We accept COD and SePay payments." },

        // Payment
        { "payment", "STEM Store supports 2 payment methods:\n• **COD** - Cash on Delivery\n• **SePay** - Bank transfer (auto-confirmed via QR scan)\n\nSePay orders expire after 10 minutes if unpaid." },
        { "pay", "We accept COD (Cash on Delivery) and SePay bank transfer. QR code is displayed on the order page after checkout." },

        // Shipping
        { "shipping", "Shipping fee: **30,000₫** per order (nationwide).\nDelivery time:\n• HCMC inner city: 1-2 days\n• Suburban areas: 3-5 days\n• Northern/Central regions: 5-7 days" },
        { "delivery", "Shipping fee: 30,000₫/order. Nationwide delivery available. Track your order in 'My Orders'." },
        { "ship", "Shipping: 30,000₫/order. Nationwide delivery, HCMC 1-2 days." },

        // Warranty
        { "warranty", "Warranty policies at STEM Store:\n• **Standard** - 12 months (manufacturer defects)\n• **Extended** - 24 months (comprehensive)\n• **Premium** - 36 months (30-day replacement, 24/7 support)\n\nSubmit warranty claims through your account dashboard." },
        { "guarantee", "All products include manufacturer warranty. Check your warranty status in 'My Warranties' section." },

        // Coupons
        { "coupon", "Enter coupon codes at checkout. Some active codes:\n• **WELCOME2025** - 10% off (orders from 500,000₫)\n• **STUDENT15** - 15% off for students (orders from 300,000₫)\n• **FREESHIP** - Free shipping" },
        { "discount", "Check our Promotions page for active coupons. Enter code at checkout to apply discount." },
        { "promo", "We regularly offer discounts and promotions. Enter coupon codes during checkout." },

        // Account
        { "register", "To create an account:\n1. Tap 'Register'\n2. Fill in: Username, Email, Password\n3. Confirm registration\n\nAfter registering, you can place orders and track shipments." },
        { "login", "Tap 'Login' → Enter Email and Password. Forgot password? Use the 'Forgot Password' link." },
        { "sign up", "Tap 'Register' to create a new account with your email. It only takes a minute!" },

        // Products
        { "arduino", "Arduino microcontroller boards at STEM Store:\n• **Arduino Uno R3** - 350,000₫\n• **Arduino Mega 2560** - 520,000₫\n• **Arduino Nano V3** - 180,000₫\n• **Arduino Starter Kit** - 1,250,000₫\n\nBrowse our Microcontrollers category for more." },
        { "raspberry", "Raspberry Pi single-board computers:\n• **Raspberry Pi 4B 4GB** - 1,450,000₫\n• **Raspberry Pi 4B 8GB** - 1,850,000₫\n• **Raspberry Pi Pico** - 85,000₫\n• **Complete Starter Kit** - 2,100,000₫" },
        { "esp32", "ESP32/ESP8266 WiFi + Bluetooth modules:\n• **ESP32 DevKit** - 120,000₫\n• **ESP32-CAM (with camera)** - 150,000₫\n• **ESP8266 NodeMCU** - 75,000₫" },
        { "sensor", "Popular sensors at STEM Store:\n• **DHT22** (temperature/humidity) - 95,000₫\n• **HC-SR04** (ultrasonic distance) - 45,000₫\n• **PIR HC-SR501** (motion) - 35,000₫\n• **MQ-2** (gas/smoke) - 55,000₫\n• **BMP280** (barometric pressure) - 65,000₫\n• **Sensor Kit 37-in-1** - 680,000₫" },

        // Contact
        { "contact", "Contact STEM Store:\n📧 Email: support@stemstore.vn\n📞 Hotline: 0901-234-567\n🕐 Hours: 8:00 AM - 6:00 PM (Mon - Sat)" },
        { "support", "Need help? Reach us at:\n📧 support@stemstore.vn\n📞 0901-234-567\nOr use this chatbot for quick answers!" },

        // Returns
        { "return", "Return policy:\n• Returns accepted within **7 days** for manufacturer defects\n• Products must be in original packaging with all accessories\n• Call our hotline 0901-234-567 for return instructions" },
        { "refund", "Refunds are processed within 7-14 business days after return confirmation. Refund via original payment method." },

        // Greetings
        { "hello", "👋 Hello! I'm STEM Bot, your AI assistant. How can I help you today? Ask about products, shipping, warranty, payment, or orders!" },
        { "hi", "👋 Hi there! I'm here to help you with anything STEM Store related. What would you like to know?" },
        { "thanks", "😊 You're welcome! Is there anything else I can help with?" },
    };

    // ===== Keywords that trigger product DB suggestions (must be real product search terms) =====
    private static readonly string[] ProductSearchKeywords =
    {
        "arduino", "esp32", "esp8266", "raspberry", "sensor", "kit",
        "module", "servo", "motor", "led", "lcd", "oled", "relay",
        "breadboard", "resistor", "microcontroller", "shield", "robot", "iot"
    };

    // ===== System Prompt =====
    private const string SystemPrompt = @"You are the AI assistant for STEM Store — a Vietnamese e-commerce shop specializing in STEM products (Science, Technology, Engineering, Mathematics) including microcontrollers (Arduino, Raspberry Pi, ESP32), sensors, modules, learning kits, and electronic accessories.

Your role:
- Recommend STEM products suitable for the customer's needs
- Provide basic technical guidance on using components
- Answer questions about ordering, payment, and warranty
- Suggest suitable combos/kits for specific projects

CRITICAL RULES — follow these strictly:
1. ALWAYS reply in English. NEVER use Vietnamese or any other language.
2. ALWAYS use Vietnamese Dong (₫) for ALL prices. Example: 350,000₫. NEVER use USD ($) or any other currency.
3. Reply concisely, clearly, and in a friendly manner.
4. If a question is outside the STEM/electronics scope, politely decline and redirect to product topics.
5. Recommend specific products when possible with prices in ₫ (Vietnamese Dong).
6. Do not fabricate information — if unsure, say so and suggest contacting support.

Price reference (Vietnamese Dong):
- Arduino Uno R3: 350,000₫ | Arduino Mega: 520,000₫ | Arduino Nano: 180,000₫
- ESP32 DevKit: 120,000₫ | ESP32-CAM: 150,000₫ | ESP8266: 75,000₫
- Raspberry Pi 4B 4GB: 1,450,000₫ | Raspberry Pi Pico: 85,000₫
- DHT22 sensor: 95,000₫ | HC-SR04: 45,000₫ | Sensor Kit 37-in-1: 680,000₫
- Shipping fee: 30,000₫ per order";

    public ChatbotService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        _apiKey = configuration["Groq:ApiKey"];
        _model = configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";
        _isConfigured = !string.IsNullOrEmpty(_apiKey);

        if (!_isConfigured)
            Console.WriteLine("[WARNING] Groq API key is missing. AI chatbot will only use FAQ responses.");
    }

    public async Task<ChatbotResponse> AskAsync(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return new ChatbotResponse { Answer = "Please enter a question.", Source = "error" };

        ChatbotResponse response;

        // Step 1: FAQ Match
        var faqAnswer = SearchFaq(question);
        if (faqAnswer != null)
        {
            response = new ChatbotResponse { Answer = faqAnswer, Source = "faq" };
        }
        else if (!_isConfigured)
        {
            response = new ChatbotResponse
            {
                Answer = "Sorry, the AI assistant is not configured yet. Please contact our hotline 0901-234-567 for support.",
                Source = "fallback"
            };
        }
        else
        {
            try
            {
                response = await CallGroqApiAsync(question);
            }
            catch (TaskCanceledException)
            {
                response = new ChatbotResponse
                {
                    Answer = "Sorry, the AI system is busy. Please try again later or call our hotline 0901-234-567.",
                    Source = "error"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Chatbot ERROR] {ex.Message}");
                response = new ChatbotResponse
                {
                    Answer = "Sorry, an error occurred. Please try again later.",
                    Source = "error"
                };
            }
        }

        // Attach real product suggestions by checking:
        // 1. The question itself (e.g. user types "arduino")
        // 2. The AI/FAQ response text (e.g. user asks "best sellers" → AI mentions "arduino")
        // This ensures product cards appear even for generic queries like "best seller products"
        if (response.Source != "error")
        {
            var keyword = ExtractProductKeyword(question)
                          ?? ExtractProductKeyword(response.Answer);
            if (keyword != null)
                response.Products = await GetProductSuggestionsAsync(keyword);
        }

        return response;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

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

    private string? ExtractProductKeyword(string question)
    {
        var lower = question.ToLowerInvariant();
        return ProductSearchKeywords.FirstOrDefault(k => lower.Contains(k));
    }

    private async Task<List<ProductSuggestion>> GetProductSuggestionsAsync(string keyword)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var productRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();

            var filter = new ProductFilter
            {
                SearchTerm = keyword,
                PageNumber = 1,
                PageSize = 3
            };

            var (items, _) = await productRepo.GetPagedProductsAsync(filter);

            var suggestions = new List<ProductSuggestion>();
            foreach (var p in items.Take(3))
            {
                // GetDetailByIdAsync includes ProductImages
                var detail = await productRepo.GetDetailByIdAsync(p.ProductId);
                var imageUrl = detail?.ProductImages
                    .FirstOrDefault(img => img.IsPrimary == true)?.ImageUrl
                    ?? detail?.ProductImages.FirstOrDefault()?.ImageUrl;

                suggestions.Add(new ProductSuggestion
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Price = p.Price,
                    ImageUrl = imageUrl
                });
            }

            return suggestions;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Chatbot] Product query error: {ex.Message}");
            return new List<ProductSuggestion>();
        }
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
            max_tokens = 512
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
