using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoToSqlExtractor
{
    class Program
    {
        // Configuration
        private static string MongoConnectionString = "mongodb://localhost:27017";
        private static string MongoDatabaseName = "LoyaltyData";
//        private static string SqlConnectionString = "Server=YourServer;Database=YourDatabase;User Id=YourUsername;Password=YourPassword;Connection Timeout=300;";

        static string SqlConnectionString = "Server=.;Database=Collect1;Integrated Security=true;TrustServerCertificate=true;Connection Timeout=60;Max Pool Size=100;";

        private static int BatchSize = 5000;
        private static bool TruncateExistingTables = false;
        private static readonly Dictionary<string, EntityTypeMapping> EntityMappings = new Dictionary<string, EntityTypeMapping>();

        static async Task Main(string[] args)
        {
            try
            {
                // Override configuration if provided
                if (args.Length >= 1) SqlConnectionString = args[0];
                if (args.Length >= 2) MongoConnectionString = args[1];
                if (args.Length >= 3) MongoDatabaseName = args[2];

                Console.WriteLine("MongoDB to SQL Server Extractor");
                Console.WriteLine("===============================");
                Console.WriteLine($"MongoDB: {MongoConnectionString}/{MongoDatabaseName}");
                Console.WriteLine($"SQL Server: {SqlConnectionString}");
                Console.WriteLine($"Batch Size: {BatchSize}");
                Console.WriteLine("===============================");

                // Initialize entity mappings
                InitializeEntityMappings();

                // Connect to MongoDB
                var mongoClient = new MongoClient(MongoConnectionString);
                var database = mongoClient.GetDatabase(MongoDatabaseName);

                // Get available entity types in MongoDB
                var availableEntityTypes = await GetAvailableEntityTypes(database);

                if (availableEntityTypes.Count == 0)
                {
                    Console.WriteLine("No collections found in MongoDB database. Please ensure data has been imported first.");
                    return;
                }

                Console.WriteLine($"Found {availableEntityTypes.Count} entity types in MongoDB:");
                foreach (var type in availableEntityTypes)
                {
                    Console.WriteLine($"- {type}");
                }

                // Process each entity type
                foreach (var entityType in availableEntityTypes)
                {
                    if (EntityMappings.ContainsKey(entityType))
                    {
                        Console.WriteLine($"\nProcessing {entityType}...");
                        await ProcessEntityType(database, entityType, EntityMappings[entityType]);
                    }
                    else
                    {
                        Console.WriteLine($"\nSkipping {entityType} - no mapping defined.");
                    }
                }

                Console.WriteLine("\nExtraction completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task<List<string>> GetAvailableEntityTypes(IMongoDatabase database)
        {
            var collections = await database.ListCollectionNamesAsync();
            return await collections.ToListAsync();
        }

        private static void InitializeEntityMappings()
        {
            // GiftCardPayment mapping
            EntityMappings["GiftCardPayment"] = new EntityTypeMapping
            {
                Description = "Gift Card Payment transactions",
                FieldMappings = new List<FieldMapping>
                {
                    // Metadata fields
                    new FieldMapping("FromID", "$_metadata.FromID", typeof(long)),
                    new FieldMapping("RawID", "$_metadata.RAWID", typeof(string)),
                    new FieldMapping("RawDate", "$_metadata.RAWDATE", typeof(DateTime)),
                    new FieldMapping("EffectiveFrom", "$_metadata.EffectiveFrom", typeof(DateTimeOffset)),
                    
                    // Card details
                    new FieldMapping("CardKey", "$Card.CardKey", typeof(string)),
                    new FieldMapping("ClubKey", "$Card.ClubKey", typeof(string)),
                    new FieldMapping("CardType", "$Card.CardType", typeof(string)),
                    new FieldMapping("CardStatus", "$Card.CardStatus", typeof(string)),
                    new FieldMapping("CardState", "$Card.CardState", typeof(string)),
                    new FieldMapping("CardBalance", "$Card.CardBalance", typeof(decimal)),
                    new FieldMapping("CardBalanceFormatted", "$Card.CardBalanceFormatted", typeof(string)),
                    new FieldMapping("BalanceCurrency", "$Card.BalanceCurrency", typeof(string)),
                    new FieldMapping("Currency", "$Card.Currency", typeof(string)),
                    new FieldMapping("CurrencySymbol", "$Card.CurrencySymbol", typeof(string)),
                    new FieldMapping("AccountType", "$Card.AccountType", typeof(string)),
                    new FieldMapping("LastRestaurantKey", "$Card.LastRestaurantKey", typeof(int)),
                    new FieldMapping("TotalLoadAmount", "$Card.TotalLoadAmount", typeof(decimal)),
                    new FieldMapping("TotalRedeemAmount", "$Card.TotalRedeemAmount", typeof(decimal)),
                    new FieldMapping("CreatedOn", "$Card.CreatedOn", typeof(DateTime)),
                    new FieldMapping("LastLoaded", "$Card.LastLoaded", typeof(DateTimeOffset)),
                    new FieldMapping("LastRedeemed", "$Card.LastRedeemed", typeof(DateTimeOffset)),
                    new FieldMapping("LastSwipe", "$Card.LastSwipe", typeof(DateTimeOffset)),
                    
                    // Customer details
                    new FieldMapping("PurchaserFirstName", "$Card.PurchaserDetails.FirstName", typeof(string)),
                    new FieldMapping("PurchaserLastName", "$Card.PurchaserDetails.LastName", typeof(string)),
                    new FieldMapping("PurchaserEmail", "$Card.PurchaserDetails.EmailAddress", typeof(string)),
                    new FieldMapping("PurchaserMobilePhone", "$Card.PurchaserDetails.MobilePhone", typeof(string)),
                    new FieldMapping("PurchaserProfileKey", "$Card.PurchaserDetails.ProfileKey", typeof(string)),
                    new FieldMapping("RecipientFirstName", "$Card.RecipientDetails.FirstName", typeof(string)),
                    new FieldMapping("RecipientLastName", "$Card.RecipientDetails.LastName", typeof(string)),
                    new FieldMapping("RecipientEmail", "$Card.RecipientDetails.EmailAddress", typeof(string)),
                    new FieldMapping("RecipientMobilePhone", "$Card.RecipientDetails.MobilePhone", typeof(string)),
                    
                    // Event details
                    new FieldMapping("Amount", "$Amount", typeof(decimal)),
                    new FieldMapping("AmountCurrency", "$AmountCurrency", typeof(string)),
                    new FieldMapping("EventName", "$EventName", typeof(string)),
                    new FieldMapping("EventDate", "$EventDate", typeof(DateTimeOffset)),
                    new FieldMapping("EntityName", "$EntityName", typeof(string)),
                    new FieldMapping("TriggerUTID", "$TriggerUTID", typeof(string)),
                    new FieldMapping("TriggerEventName", "$TriggerEventName", typeof(string)),
                    
                    // Transaction details
                    new FieldMapping("TransactingAccountKey", "$TransactingAccountDetail.AccountKey", typeof(string)),
                    new FieldMapping("TransactingAccountName", "$TransactingAccountDetail.AccountName", typeof(string)),
                    new FieldMapping("TransactingMerchantKey", "$TransactingAccountDetail.MerchantKey", typeof(int)),
                    new FieldMapping("TransactingAuthorizedBy", "$TransactingAccountDetail.AuthorizedBy", typeof(string)),
                    new FieldMapping("TransactingRestaurantKey", "$TransactingAccountDetail.RestaurantKey", typeof(int)),
                    new FieldMapping("TransactingAccountProfileKey", "$TransactingAccountDetail.AccountProfileKey", typeof(string)),
                    
                    // API details
                    new FieldMapping("UTID", "$APIBody.UTID", typeof(string)),
                    new FieldMapping("APIBrandKey", "$APIBody.BrandKey", typeof(string)),
                    new FieldMapping("APIOriginator", "$APIBody.Originator", typeof(string)),
                    new FieldMapping("APIRestaurantKey", "$APIBody.RestaurantKey", typeof(int)),
                    new FieldMapping("APIRestaurantName", "$APIBody.RestaurantName", typeof(string)),
                    new FieldMapping("APIOrderingChannel", "$APIBody.OrderingChannel", typeof(string)),
                    new FieldMapping("APIFulfilmentChannel", "$APIBody.FulfilmentChannel", typeof(string)),
                    new FieldMapping("APIOriginatorPosID", "$APIBody.OriginatorPosID", typeof(string)),
                    new FieldMapping("APIOriginatorSiteID", "$APIBody.OriginatorSiteID", typeof(string)),
                    new FieldMapping("APIOriginatorEmployeeID", "$APIBody.OriginatorEmployeeID", typeof(string)),
                    new FieldMapping("APIOriginatorInvoiceNumber", "$APIBody.OriginatorInvoiceNumber", typeof(string)),
                    new FieldMapping("APIOriginatorTransactionID", "$APIBody.OriginatorTransactionID", typeof(string)),
                    new FieldMapping("APIRestaurantCountryCode", "$APIBody.RestaurantCountryCode", typeof(string))
                }
            };

            // GiftCardPaymentManual mapping
            EntityMappings["GiftCardPaymentManual"] = new EntityTypeMapping
            {
                Description = "Manual Gift Card Payment transactions",
                FieldMappings = new List<FieldMapping>
                {
                    // Metadata fields
                    new FieldMapping("FromID", "$_metadata.FromID", typeof(long)),
                    new FieldMapping("RawID", "$_metadata.RAWID", typeof(string)),
                    new FieldMapping("RawDate", "$_metadata.RAWDATE", typeof(DateTime)),
                    new FieldMapping("EffectiveFrom", "$_metadata.EffectiveFrom", typeof(DateTimeOffset)),
                    
                    // Card details
                    new FieldMapping("CardKey", "$Card.CardKey", typeof(string)),
                    new FieldMapping("ClubKey", "$Card.ClubKey", typeof(string)),
                    new FieldMapping("CardType", "$Card.CardType", typeof(string)),
                    new FieldMapping("CardStatus", "$Card.CardStatus", typeof(string)),
                    new FieldMapping("CardState", "$Card.CardState", typeof(string)),
                    new FieldMapping("CardBalance", "$Card.CardBalance", typeof(decimal)),
                    new FieldMapping("CardBalanceFormatted", "$Card.CardBalanceFormatted", typeof(string)),
                    new FieldMapping("BalanceCurrency", "$Card.BalanceCurrency", typeof(string)),
                    new FieldMapping("Currency", "$Card.Currency", typeof(string)),
                    new FieldMapping("CurrencySymbol", "$Card.CurrencySymbol", typeof(string)),
                    new FieldMapping("AccountType", "$Card.AccountType", typeof(string)),
                    new FieldMapping("LastRestaurantKey", "$Card.LastRestaurantKey", typeof(int)),
                    new FieldMapping("LastRestaurant", "$Card.LastRestaurant", typeof(string)),
                    new FieldMapping("TotalLoadAmount", "$Card.TotalLoadAmount", typeof(decimal)),
                    new FieldMapping("TotalRedeemAmount", "$Card.TotalRedeemAmount", typeof(decimal)),
                    new FieldMapping("CreatedOn", "$Card.CreatedOn", typeof(DateTime)),
                    new FieldMapping("LastLoaded", "$Card.LastLoaded", typeof(DateTimeOffset)),
                    new FieldMapping("LastRedeemed", "$Card.LastRedeemed", typeof(DateTimeOffset)),
                    new FieldMapping("LastSwipe", "$Card.LastSwipe", typeof(DateTimeOffset)),
                    
                    // Customer details
                    new FieldMapping("PurchaserFirstName", "$Card.PurchaserDetails.FirstName", typeof(string)),
                    new FieldMapping("PurchaserLastName", "$Card.PurchaserDetails.LastName", typeof(string)),
                    new FieldMapping("PurchaserEmail", "$Card.PurchaserDetails.EmailAddress", typeof(string)),
                    new FieldMapping("PurchaserMobilePhone", "$Card.PurchaserDetails.MobilePhone", typeof(string)),
                    new FieldMapping("PurchaserProfileKey", "$Card.PurchaserDetails.ProfileKey", typeof(string)),
                    
                    // Event details
                    new FieldMapping("Amount", "$Amount", typeof(decimal)),
                    new FieldMapping("AmountCurrency", "$AmountCurrency", typeof(string)),
                    new FieldMapping("EventName", "$EventName", typeof(string)),
                    new FieldMapping("EventDate", "$EventDate", typeof(DateTimeOffset)),
                    new FieldMapping("EntityName", "$EntityName", typeof(string)),
                    new FieldMapping("TriggerUTID", "$TriggerUTID", typeof(string)),
                    new FieldMapping("TriggerEventName", "$TriggerEventName", typeof(string)),
                    
                    // Transaction details
                    new FieldMapping("TransactingAccountKey", "$TransactingAccountDetail.AccountKey", typeof(string)),
                    new FieldMapping("TransactingAccountName", "$TransactingAccountDetail.AccountName", typeof(string)),
                    new FieldMapping("TransactingMerchantKey", "$TransactingAccountDetail.MerchantKey", typeof(int)),
                    new FieldMapping("TransactingAuthorizedBy", "$TransactingAccountDetail.AuthorizedBy", typeof(string)),
                    new FieldMapping("TransactingRestaurantKey", "$TransactingAccountDetail.RestaurantKey", typeof(int)),
                    new FieldMapping("TransactingAccountProfileKey", "$TransactingAccountDetail.AccountProfileKey", typeof(string)),
                    
                    // API Body details
                    new FieldMapping("StoreValue", "$APIBody.value", typeof(decimal)),
                    new FieldMapping("Comment", "$APIBody.comment", typeof(string)),
                    new FieldMapping("ActorKey", "$APIBody.actor_key", typeof(string)),
                    new FieldMapping("ActorName", "$APIBody.actor_name", typeof(string)),
                    new FieldMapping("WaitronID", "$APIBody.waitron_id", typeof(string)),
                    new FieldMapping("ReceiptNumber", "$APIBody.receipt_number", typeof(string)),
                    new FieldMapping("StoreBrandName", "$APIBody.store.brand_name", typeof(string)),
                    new FieldMapping("StoreAccountKey", "$APIBody.store.account_key", typeof(string)),
                    new FieldMapping("StoreRestaurantKey", "$APIBody.store.restaurant_key", typeof(string)),
                    new FieldMapping("StoreAccountDisplayName", "$APIBody.store.account_displayname", typeof(string))
                }
            };

            // GiftCardLoadB2B mapping
            EntityMappings["GiftCardLoadB2B"] = new EntityTypeMapping
            {
                Description = "Gift Card Load B2B transactions",
                FieldMappings = new List<FieldMapping>
                {
                    // Metadata fields
                    new FieldMapping("FromID", "$_metadata.FromID", typeof(long)),
                    new FieldMapping("RawID", "$_metadata.RAWID", typeof(string)),
                    new FieldMapping("RawDate", "$_metadata.RAWDATE", typeof(DateTime)),
                    new FieldMapping("EffectiveFrom", "$_metadata.EffectiveFrom", typeof(DateTimeOffset)),
                    
                    // Card details
                    new FieldMapping("CardKey", "$Card.CardKey", typeof(string)),
                    new FieldMapping("ClubKey", "$Card.ClubKey", typeof(string)),
                    new FieldMapping("ClubName", "$Card.ClubName", typeof(string)),
                    new FieldMapping("CardType", "$Card.CardType", typeof(string)),
                    new FieldMapping("CardStatus", "$Card.CardStatus", typeof(string)),
                    new FieldMapping("CardState", "$Card.CardState", typeof(string)),
                    new FieldMapping("CardBalance", "$Card.CardBalance", typeof(decimal)),
                    new FieldMapping("CardBalanceFormatted", "$Card.CardBalanceFormatted", typeof(string)),
                    new FieldMapping("CreatedOn", "$Card.CreatedOn", typeof(DateTime)),
                    new FieldMapping("LastLoaded", "$Card.LastLoaded", typeof(DateTimeOffset)),
                    new FieldMapping("BalanceEffectiveFrom", "$Card.BalanceEffectiveFrom", typeof(DateTimeOffset)),
                    new FieldMapping("TotalLoadAmount", "$Card.TotalLoadAmount", typeof(decimal)),
                    new FieldMapping("AccountProfileKey", "$Card.AccountProfileKey", typeof(string)),
                    
                    // Customer details
                    new FieldMapping("PurchaserFirstName", "$Card.PurchaserDetails.FirstName", typeof(string)),
                    new FieldMapping("PurchaserLastName", "$Card.PurchaserDetails.LastName", typeof(string)),
                    new FieldMapping("PurchaserProfileKey", "$Card.PurchaserDetails.ProfileKey", typeof(string)),
                    new FieldMapping("PurchaserMobilePhone", "$Card.PurchaserDetails.MobilePhone", typeof(string)),
                    new FieldMapping("PurchaserEmailAddress", "$Card.PurchaserDetails.EmailAddress", typeof(string)),
                    
                    // Load details
                    new FieldMapping("Amount", "$Amount", typeof(decimal)),
                    new FieldMapping("EventDate", "$EventDate", typeof(DateTimeOffset)),
                    new FieldMapping("EventName", "$EventName", typeof(string)),
                    new FieldMapping("EntityName", "$EntityName", typeof(string)),
                    new FieldMapping("AmountCurrency", "$AmountCurrency", typeof(string)),
                    new FieldMapping("TriggerEventName", "$TriggerEventName", typeof(string)),
                    
                    // Transaction details
                    new FieldMapping("TransactingAccountKey", "$TransactingAccountDetail.AccountKey", typeof(decimal)),
                    new FieldMapping("TransactingAccountName", "$TransactingAccountDetail.AccountName", typeof(string)),
                    new FieldMapping("TransactingMerchantKey", "$TransactingAccountDetail.MerchantKey", typeof(decimal)),
                    
                    // API Body details
                    new FieldMapping("ApiValue", "$APIBody.value", typeof(decimal)),
                    new FieldMapping("ApiDatetime", "$APIBody.datetime", typeof(DateTimeOffset)),
                    new FieldMapping("ApiEventName", "$APIBody.event_name", typeof(string)),
                    new FieldMapping("B2BCardKey", "$APIBody.b2b_card.key", typeof(string)),
                    new FieldMapping("B2BCardValue", "$APIBody.b2b_card.value", typeof(decimal)),
                    new FieldMapping("B2BCardNumber", "$APIBody.b2b_card.card_number", typeof(string)),
                    new FieldMapping("B2BBatchType", "$APIBody.b2b_batch.type", typeof(string)),
                    new FieldMapping("B2BBatchAction", "$APIBody.b2b_batch.action", typeof(string)),
                    new FieldMapping("B2BBatchStatus", "$APIBody.b2b_batch.status", typeof(string)),
                    new FieldMapping("B2BBatchBrandKey", "$APIBody.b2b_batch.brandkey", typeof(string)),
                    new FieldMapping("B2BBatchKey", "$APIBody.b2b_batch.batch_key", typeof(string)),
                    new FieldMapping("B2BBatchMerchantName", "$APIBody.b2b_batch.merchant_name", typeof(string)),
                    new FieldMapping("B2BBatchMerchantEmail", "$APIBody.b2b_batch.merchant_email", typeof(string)),
                    new FieldMapping("B2BBatchReceiptNumber", "$APIBody.b2b_batch.receipt_number", typeof(string))
                }
            };

            // LoyaltyCardPointsEarn mapping
            EntityMappings["LoyaltyCardPointsEarn"] = new EntityTypeMapping
            {
                Description = "Loyalty Card Points Earn transactions",
                FieldMappings = new List<FieldMapping>
                {
                    // Metadata fields
                    new FieldMapping("FromID", "$_metadata.FromID", typeof(long)),
                    new FieldMapping("RawID", "$_metadata.RAWID", typeof(string)),
                    new FieldMapping("RawDate", "$_metadata.RAWDATE", typeof(DateTime)),
                    new FieldMapping("EffectiveFrom", "$_metadata.EffectiveFrom", typeof(DateTimeOffset)),
                    
                    // Card details
                    new FieldMapping("CardKey", "$Card.CardKey", typeof(string)),
                    new FieldMapping("ClubKey", "$Card.ClubKey", typeof(string)),
                    new FieldMapping("CardType", "$Card.CardType", typeof(string)),
                    new FieldMapping("CardState", "$Card.CardState", typeof(string)),
                    new FieldMapping("CardStatus", "$Card.CardStatus", typeof(string)),
                    new FieldMapping("CardBalance", "$Card.CardBalance", typeof(decimal)),
                    new FieldMapping("CardBalanceFormatted", "$Card.CardBalanceFormatted", typeof(string)),
                    new FieldMapping("BalanceCurrency", "$Card.BalanceCurrency", typeof(string)),
                    new FieldMapping("CardProfileKey", "$Card.ProfileKey", typeof(string)),
                    new FieldMapping("CreatedOn", "$Card.CreatedOn", typeof(DateTime)),
                    new FieldMapping("LastSwipe", "$Card.LastSwipe", typeof(DateTimeOffset)),
                    new FieldMapping("LastEarned", "$Card.LastEarned", typeof(DateTimeOffset)),
                    new FieldMapping("LastIssued", "$Card.LastIssued", typeof(DateTimeOffset)),
                    new FieldMapping("LastRedeemed", "$Card.LastRedeemed", typeof(DateTimeOffset)),
                    new FieldMapping("LastRestaurant", "$Card.LastRestaurant", typeof(string)),
                    new FieldMapping("LastRestaurantKey", "$Card.LastRestaurantKey", typeof(int)),
                    new FieldMapping("ProfileBrandGroupKey", "$Card.ProfileBrandGroupKey", typeof(string)),
                    
                    // Event details
                    new FieldMapping("EventDate", "$EventDate", typeof(DateTimeOffset)),
                    new FieldMapping("EventName", "$EventName", typeof(string)),
                    new FieldMapping("EntityName", "$EntityName", typeof(string)),
                    new FieldMapping("TriggerUTID", "$TriggerUTID", typeof(string)),
                    new FieldMapping("TriggerEventName", "$TriggerEventName", typeof(string)),
                    
                    // Points details
                    new FieldMapping("PointsEarned", "$PointsEarned", typeof(decimal)),
                    new FieldMapping("CardBalanceAfter", "$CardBalanceAfter", typeof(decimal)),
                    new FieldMapping("CardBalanceBefore", "$CardBalanceBefore", typeof(decimal)),
                    new FieldMapping("VouchersIssued", "$VouchersIssued", typeof(int)),
                    
                    // API Body details
                    new FieldMapping("UTID", "$APIBody.UTID", typeof(string)),
                    new FieldMapping("BrandKey", "$APIBody.BrandKey", typeof(string)),
                    new FieldMapping("Originator", "$APIBody.Originator", typeof(string)),
                    new FieldMapping("RestaurantKey", "$APIBody.RestaurantKey", typeof(int)),
                    new FieldMapping("RestaurantName", "$APIBody.RestaurantName", typeof(string)),
                    new FieldMapping("OrderingChannel", "$APIBody.OrderingChannel", typeof(string)),
                    new FieldMapping("OriginatorPosID", "$APIBody.OriginatorPosID", typeof(string)),
                    new FieldMapping("OriginatorSiteID", "$APIBody.OriginatorSiteID", typeof(string)),
                    new FieldMapping("OriginatorEmployeeID", "$APIBody.OriginatorEmployeeID", typeof(string)),
                    new FieldMapping("OriginatorInvoiceNumber", "$APIBody.OriginatorInvoiceNumber", typeof(string)),
                    new FieldMapping("OriginatorTransactionID", "$APIBody.OriginatorTransactionID", typeof(string)),
                    new FieldMapping("ItemsTotal", "$APIBody.ItemsTotal", typeof(decimal)),
                    new FieldMapping("ItemsTotalExclTax", "$APIBody.ItemsTotalExclTax", typeof(decimal)),
                    new FieldMapping("ItemsTotalBeforeDiscounts", "$APIBody.ItemsTotalBeforeDiscounts", typeof(decimal))
                }
            };

            // LoyaltyCardPointsEarnManual mapping
            EntityMappings["LoyaltyCardPointsEarnManual"] = new EntityTypeMapping
            {
                Description = "Manual Loyalty Card Points Earn transactions",
                FieldMappings = new List<FieldMapping>
                {
                    // Metadata fields
                    new FieldMapping("FromID", "$_metadata.FromID", typeof(long)),
                    new FieldMapping("RawID", "$_metadata.RAWID", typeof(string)),
                    new FieldMapping("RawDate", "$_metadata.RAWDATE", typeof(DateTime)),
                    new FieldMapping("EffectiveFrom", "$_metadata.EffectiveFrom", typeof(DateTimeOffset)),
                    
                    // Card details
                    new FieldMapping("CardKey", "$Card.CardKey", typeof(string)),
                    new FieldMapping("ClubKey", "$Card.ClubKey", typeof(string)),
                    new FieldMapping("CardType", "$Card.CardType", typeof(string)),
                    new FieldMapping("CardState", "$Card.CardState", typeof(string)),
                    new FieldMapping("CardStatus", "$Card.CardStatus", typeof(string)),
                    new FieldMapping("CardBalance", "$Card.CardBalance", typeof(decimal)),
                    new FieldMapping("CardBalanceFormatted", "$Card.CardBalanceFormatted", typeof(string)),
                    new FieldMapping("BalanceCurrency", "$Card.BalanceCurrency", typeof(string)),
                    new FieldMapping("CardProfileKey", "$Card.ProfileKey", typeof(string)),
                    new FieldMapping("LastSwipe", "$Card.LastSwipe", typeof(DateTimeOffset)),
                    new FieldMapping("LastEarned", "$Card.LastEarned", typeof(DateTimeOffset)),
                    new FieldMapping("LastIssued", "$Card.LastIssued", typeof(DateTimeOffset)),
                    new FieldMapping("LastRedeemed", "$Card.LastRedeemed", typeof(DateTimeOffset)),
                    new FieldMapping("LastRestaurant", "$Card.LastRestaurant", typeof(string)),
                    new FieldMapping("LastRestaurantKey", "$Card.LastRestaurantKey", typeof(int)),
                    new FieldMapping("ProfileBrandGroupKey", "$Card.ProfileBrandGroupKey", typeof(string)),
                    
                    // Event details
                    new FieldMapping("EventDate", "$EventDate", typeof(DateTimeOffset)),
                    new FieldMapping("EventName", "$EventName", typeof(string)),
                    new FieldMapping("EntityName", "$EntityName", typeof(string)),
                    new FieldMapping("TriggerEventName", "$TriggerEventName", typeof(string)),
                    
                    // Points details
                    new FieldMapping("PointsEarned", "$PointsEarned", typeof(decimal)),
                    new FieldMapping("CardBalanceAfter", "$CardBalanceAfter", typeof(decimal)),
                    new FieldMapping("CardBalanceBefore", "$CardBalanceBefore", typeof(decimal)),
                    new FieldMapping("VouchersIssued", "$VouchersIssued", typeof(int)),
                    new FieldMapping("PointsConversionRatio", "$PointsConversionRatio", typeof(decimal)),
                    
                    // API Body details
                    new FieldMapping("Comment", "$APIBody.comment", typeof(string)),
                    new FieldMapping("CardCount", "$APIBody.card_count", typeof(string)),
                    new FieldMapping("WaitronId", "$APIBody.waitron_id", typeof(string)),
                    new FieldMapping("BillAmount", "$APIBody.bill_amount", typeof(string)),
                    new FieldMapping("ReceiptNumber", "$APIBody.receipt_number", typeof(string)),
                    new FieldMapping("GrossBillAmount", "$APIBody.gross_bill_amount", typeof(string)),
                    new FieldMapping("StoreBrandName", "$APIBody.store.brand_name", typeof(string)),
                    new FieldMapping("StoreAccountKey", "$APIBody.store.account_key", typeof(string)),
                    new FieldMapping("StoreAddressCity", "$APIBody.store.address_city", typeof(string)),
                    new FieldMapping("StoreRestaurantKey", "$APIBody.store.restaurant_key", typeof(string)),
                    new FieldMapping("StoreAddressSuburb", "$APIBody.store.address_suburb", typeof(string)),
                    new FieldMapping("StoreAddressProvince", "$APIBody.store.address_province", typeof(string)),
                    new FieldMapping("StoreLocationCountry", "$APIBody.store.location_country", typeof(string)),
                    new FieldMapping("StoreAccountDisplayName", "$APIBody.store.account_displayname", typeof(string)),
                    new FieldMapping("StoreAccountProfileKey", "$APIBody.store.account_profile_key", typeof(string))
                }
            };

            // LoyaltyCardPointsConvert mapping
            EntityMappings["LoyaltyCardPointsConvert"] = new EntityTypeMapping
            {
                Description = "Loyalty Card Points Convert to Voucher transactions",
                FieldMappings = new List<FieldMapping>
                {
                    // Metadata fields
                    new FieldMapping("FromID", "$_metadata.FromID", typeof(long)),
                    new FieldMapping("RawID", "$_metadata.RAWID", typeof(string)),
                    new FieldMapping("RawDate", "$_metadata.RAWDATE", typeof(DateTime)),
                    new FieldMapping("EffectiveFrom", "$_metadata.EffectiveFrom", typeof(DateTimeOffset)),
                    
                    // Card details
                    new FieldMapping("CardKey", "$Card.CardKey", typeof(string)),
                    new FieldMapping("ClubKey", "$Card.ClubKey", typeof(string)),
                    new FieldMapping("CardType", "$Card.CardType", typeof(string)),
                    new FieldMapping("CardState", "$Card.CardState", typeof(string)),
                    new FieldMapping("CardStatus", "$Card.CardStatus", typeof(string)),
                    new FieldMapping("CardBalance", "$Card.CardBalance", typeof(decimal)),
                    new FieldMapping("CardBalanceFormatted", "$Card.CardBalanceFormatted", typeof(string)),
                    new FieldMapping("BalanceCurrency", "$Card.BalanceCurrency", typeof(string)),
                    new FieldMapping("CardProfileKey", "$Card.ProfileKey", typeof(string)),
                    
                    new FieldMapping("AccountState", "$Card.AccountState", typeof(string)),
                    new FieldMapping("AccountStatus", "$Card.AccountStatus", typeof(string)),
                    new FieldMapping("LastEarned", "$Card.LastEarned", typeof(DateTimeOffset)),
                    new FieldMapping("LastIssued", "$Card.LastIssued", typeof(DateTimeOffset)),
                    new FieldMapping("LastSwipe", "$Card.LastSwipe", typeof(DateTimeOffset)),
                    new FieldMapping("LastRestaurant", "$Card.LastRestaurant", typeof(string)),
                    new FieldMapping("LastRestaurantKey", "$Card.LastRestaurantKey", typeof(int)),
                    new FieldMapping("ProfileBrandGroupKey", "$Card.ProfileBrandGroupKey", typeof(string)),
                    
                    // Voucher details
                    new FieldMapping("VoucherCode", "$Voucher.VoucherCode", typeof(string)),
                    new FieldMapping("VoucherName", "$Voucher.VoucherName", typeof(string)),
                    new FieldMapping("VoucherType", "$Voucher.VoucherType", typeof(string)),
                    new FieldMapping("VoucherStatus", "$Voucher.Status", typeof(string)),
                    new FieldMapping("VoucherState", "$Voucher.State", typeof(string)),
                    new FieldMapping("VoucherClubKey", "$Voucher.ClubKey", typeof(string)),
                    new FieldMapping("VoucherClubName", "$Voucher.ClubName", typeof(string)),
                    new FieldMapping("VoucherCurrency", "$Voucher.Currency", typeof(string)),
                    new FieldMapping("VoucherAmount", "$Voucher.VoucherAmount", typeof(decimal)),
                    new FieldMapping("VoucherCreateDate", "$Voucher.CreateDate", typeof(string)),
                    new FieldMapping("VoucherExpiryDate", "$Voucher.ExpiryDate", typeof(string)),
                    
                    // Event details
                    new FieldMapping("EventDate", "$EventDate", typeof(DateTimeOffset)),
                    new FieldMapping("EventName", "$EventName", typeof(string)),
                    new FieldMapping("EntityName", "$EntityName", typeof(string)),
                    new FieldMapping("TriggerUTID", "$TriggerUTID", typeof(string)),
                    new FieldMapping("TriggerEventName", "$TriggerEventName", typeof(string)),
                    new FieldMapping("TriggerCardKey", "$TriggerCardKey", typeof(string)),
                    new FieldMapping("PointsConverted", "$PointsConverted", typeof(int)),
                    new FieldMapping("CardBalanceBefore", "$CardBalanceBefore", typeof(int))
                }
            };

            // LoyaltyVoucherRedeem mapping
            EntityMappings["LoyaltyVoucherRedeem"] = new EntityTypeMapping
            {
                Description = "Loyalty Voucher Redemption transactions",
                FieldMappings = new List<FieldMapping>
                {
                    // Metadata fields
                    new FieldMapping("FromID", "$_metadata.FromID", typeof(long)),
                    new FieldMapping("RawID", "$_metadata.RAWID", typeof(string)),
                    new FieldMapping("RawDate", "$_metadata.RAWDATE", typeof(DateTime)),
                    new FieldMapping("EffectiveFrom", "$_metadata.EffectiveFrom", typeof(DateTimeOffset)),
                    
                    // Card details
                    new FieldMapping("CardKey", "$Card.CardKey", typeof(string)),
                    new FieldMapping("ClubKey", "$Card.ClubKey", typeof(string)),
                    new FieldMapping("CardType", "$Card.CardType", typeof(string)),
                    new FieldMapping("CardState", "$Card.CardState", typeof(string)),
                    new FieldMapping("CardStatus", "$Card.CardStatus", typeof(string)),
                    new FieldMapping("CardBalance", "$Card.CardBalance", typeof(decimal)),
                    new FieldMapping("CardBalanceFormatted", "$Card.CardBalanceFormatted", typeof(string)),
                    new FieldMapping("BalanceCurrency", "$Card.BalanceCurrency", typeof(string)),
                    new FieldMapping("CardProfileKey", "$Card.ProfileKey", typeof(string)),
                    new FieldMapping("CreatedOn", "$Card.CreatedOn", typeof(string)),
                    new FieldMapping("LastSwipe", "$Card.LastSwipe", typeof(DateTimeOffset)),
                    new FieldMapping("LastEarned", "$Card.LastEarned", typeof(DateTimeOffset)),
                    new FieldMapping("LastIssued", "$Card.LastIssued", typeof(DateTimeOffset)),
                    new FieldMapping("LastRedeemed", "$Card.LastRedeemed", typeof(DateTimeOffset)),
                    new FieldMapping("LastRestaurant", "$Card.LastRestaurant", typeof(string)),
                    new FieldMapping("LastRestaurantKey", "$Card.LastRestaurantKey", typeof(int)),
                    new FieldMapping("ProfileBrandGroupKey", "$Card.ProfileBrandGroupKey", typeof(string)),
                    
                    // Voucher details
                    new FieldMapping("VoucherCode", "$Voucher.VoucherCode", typeof(string)),
                    new FieldMapping("VoucherName", "$Voucher.VoucherName", typeof(string)),
                    new FieldMapping("VoucherType", "$Voucher.VoucherType", typeof(string)),
                    new FieldMapping("VoucherStatus", "$Voucher.Status", typeof(string)),
                    new FieldMapping("VoucherState", "$Voucher.State", typeof(string)),
                    new FieldMapping("VoucherClubKey", "$Voucher.ClubKey", typeof(string)),
                    new FieldMapping("VoucherClubName", "$Voucher.ClubName", typeof(string)),
                    new FieldMapping("VoucherCurrency", "$Voucher.Currency", typeof(string)),
                    new FieldMapping("VoucherAmount", "$Voucher.VoucherAmount", typeof(decimal)),
                    new FieldMapping("VoucherAmountFormatted", "$Voucher.VoucherAmountFormatted", typeof(string)),
                    new FieldMapping("VoucherCreateDate", "$Voucher.CreateDate", typeof(string)),
                    new FieldMapping("VoucherExpiryDate", "$Voucher.ExpiryDate", typeof(string)),
                    new FieldMapping("VoucherRedeemDate", "$Voucher.RedeemDate", typeof(DateTimeOffset)),
                    new FieldMapping("VoucherRedeemUTID", "$Voucher.RedeemUTID", typeof(string)),
                    
                    // Event details
                    new FieldMapping("EventDate", "$EventDate", typeof(DateTimeOffset)),
                    new FieldMapping("EventName", "$EventName", typeof(string)),
                    new FieldMapping("EntityName", "$EntityName", typeof(string)),
                    new FieldMapping("ProfileKey", "$ProfileKey", typeof(string)),
                    new FieldMapping("RedeemValue", "$RedeemValue", typeof(decimal)),
                    new FieldMapping("RedeemCurrency", "$RedeemCurrency", typeof(string)),
                    new FieldMapping("TriggerUTID", "$TriggerUTID", typeof(string)),
                    new FieldMapping("TriggerEventName", "$TriggerEventName", typeof(string)),
                    
                    // API Body details
                    new FieldMapping("APIUTID", "$APIBody.UTID", typeof(string)),
                    new FieldMapping("APIReserve", "$APIBody.Reserve", typeof(bool)),
                    new FieldMapping("APIBrandKey", "$APIBody.BrandKey", typeof(string)),
                    new FieldMapping("APICardKey", "$APIBody.CardKey", typeof(string)),
                    new FieldMapping("APIOriginator", "$APIBody.Originator", typeof(string)),
                    new FieldMapping("APIProfileKey", "$APIBody.ProfileKey", typeof(string)),
                    new FieldMapping("APIMobilePhone", "$APIBody.MobilePhone", typeof(string)),
                    new FieldMapping("APIRestaurantKey", "$APIBody.RestaurantKey", typeof(int)),
                    new FieldMapping("APIRestaurantName", "$APIBody.RestaurantName", typeof(string)),
                    new FieldMapping("APIOrderingChannel", "$APIBody.OrderingChannel", typeof(string)),
                    new FieldMapping("APIOriginatorPosID", "$APIBody.OriginatorPosID", typeof(string)),
                    new FieldMapping("APIOriginatorSiteID", "$APIBody.OriginatorSiteID", typeof(string)),
                    new FieldMapping("APIFulfilmentChannel", "$APIBody.FulfilmentChannel", typeof(string)),
                    new FieldMapping("APIBillAmountCurrency", "$APIBody.BillAmountCurrency", typeof(string)),
                    new FieldMapping("APIBillAmountRemaining", "$APIBody.BillAmountRemaining", typeof(decimal)),
                    new FieldMapping("APIOriginatorEmployeeID", "$APIBody.OriginatorEmployeeID", typeof(string)),
                    new FieldMapping("APIProfileBrandGroupKey", "$APIBody.ProfileBrandGroupKey", typeof(string)),
                    new FieldMapping("APIOriginatorTradingDate", "$APIBody.OriginatorTradingDate", typeof(DateTimeOffset)),
                    new FieldMapping("APIRestaurantCountryCode", "$APIBody.RestaurantCountryCode", typeof(string)),
                    new FieldMapping("APIOriginatorTransactionID", "$APIBody.OriginatorTransactionID", typeof(string))
                }
            };

            // LoyaltyVoucherRedeemManual mapping
            EntityMappings["LoyaltyVoucherRedeemManual"] = new EntityTypeMapping
            {
                Description = "Manual Loyalty Voucher Redemption transactions",
                FieldMappings = new List<FieldMapping>
                {
                    // Metadata fields
                    new FieldMapping("FromID", "$_metadata.FromID", typeof(long)),
                    new FieldMapping("RawID", "$_metadata.RAWID", typeof(string)),
                    new FieldMapping("RawDate", "$_metadata.RAWDATE", typeof(DateTime)),
                    new FieldMapping("EffectiveFrom", "$_metadata.EffectiveFrom", typeof(DateTimeOffset)),
                    
                    // Card details
                    new FieldMapping("CardKey", "$Card.CardKey", typeof(string)),
                    new FieldMapping("ClubKey", "$Card.ClubKey", typeof(string)),
                    new FieldMapping("CardType", "$Card.CardType", typeof(string)),
                    new FieldMapping("CardState", "$Card.CardState", typeof(string)),
                    new FieldMapping("CardStatus", "$Card.CardStatus", typeof(string)),
                    new FieldMapping("CardBalance", "$Card.CardBalance", typeof(decimal)),
                    new FieldMapping("CardBalanceFormatted", "$Card.CardBalanceFormatted", typeof(string)),
                    new FieldMapping("BalanceCurrency", "$Card.BalanceCurrency", typeof(string)),
                    new FieldMapping("CardProfileKey", "$Card.ProfileKey", typeof(string)),
                    new FieldMapping("LastSwipe", "$Card.LastSwipe", typeof(DateTimeOffset)),
                    new FieldMapping("LastEarned", "$Card.LastEarned", typeof(DateTimeOffset)),
                    new FieldMapping("LastIssued", "$Card.LastIssued", typeof(DateTimeOffset)),
                    new FieldMapping("LastRedeemed", "$Card.LastRedeemed", typeof(DateTimeOffset)),
                    new FieldMapping("LastRestaurant", "$Card.LastRestaurant", typeof(string)),
                    new FieldMapping("LastRestaurantKey", "$Card.LastRestaurantKey", typeof(int)),
                    
                    // Voucher details
                    new FieldMapping("VoucherCode", "$Voucher.VoucherCode", typeof(string)),
                    new FieldMapping("VoucherName", "$Voucher.VoucherName", typeof(string)),
                    new FieldMapping("VoucherType", "$Voucher.VoucherType", typeof(string)),
                    new FieldMapping("VoucherStatus", "$Voucher.Status", typeof(string)),
                    new FieldMapping("VoucherState", "$Voucher.State", typeof(string)),
                    new FieldMapping("VoucherClubKey", "$Voucher.ClubKey", typeof(string)),
                    new FieldMapping("VoucherClubName", "$Voucher.ClubName", typeof(string)),
                    new FieldMapping("VoucherCurrency", "$Voucher.Currency", typeof(string)),
                    new FieldMapping("VoucherAmount", "$Voucher.VoucherAmount", typeof(decimal)),
                    new FieldMapping("VoucherCreateDate", "$Voucher.CreateDate", typeof(string)),
                    new FieldMapping("VoucherExpiryDate", "$Voucher.ExpiryDate", typeof(string)),
                    new FieldMapping("VoucherRedeemDate", "$Voucher.RedeemDate", typeof(DateTimeOffset)),
                    
                    // Event details
                    new FieldMapping("EventDate", "$EventDate", typeof(DateTimeOffset)),
                    new FieldMapping("EventName", "$EventName", typeof(string)),
                    new FieldMapping("EntityName", "$EntityName", typeof(string)),
                    new FieldMapping("ProfileKey", "$ProfileKey", typeof(string)),
                    new FieldMapping("RedeemValue", "$RedeemValue", typeof(decimal)),
                    new FieldMapping("RedeemCurrency", "$RedeemCurrency", typeof(string)),
                    new FieldMapping("TriggerEventName", "$TriggerEventName", typeof(string)),
                    
                    // Transaction details
                    new FieldMapping("TransactingAccountKey", "$TransactingAccountDetail.AccountKey", typeof(int)),
                    new FieldMapping("TransactingAccountName", "$TransactingAccountDetail.AccountName", typeof(string)),
                    new FieldMapping("TransactingAuthorizedBy", "$TransactingAccountDetail.AuthorizedBy", typeof(string)),
                    new FieldMapping("TransactingRestaurantKey", "$TransactingAccountDetail.RestaurantKey", typeof(int)),
                    
                    // API Body details
                    new FieldMapping("Comment", "$APIBody.comment", typeof(string)),
                    new FieldMapping("WaitronId", "$APIBody.waitron_id", typeof(string)),
                    new FieldMapping("CardCount", "$APIBody.card_count", typeof(string)),
                    new FieldMapping("BillAmount", "$APIBody.bill_amount", typeof(string)),
                    new FieldMapping("ReceiptNumber", "$APIBody.receipt_number", typeof(string)),
                    new FieldMapping("GrossBillAmount", "$APIBody.gross_bill_amount", typeof(string))
                }
            };

            // LoyaltyVoucherRedeemReversal mapping
            EntityMappings["LoyaltyVoucherRedeemReversal"] = new EntityTypeMapping
            {
                Description = "Loyalty Voucher Redemption Reversal transactions",
                FieldMappings = new List<FieldMapping>
                {
                    // Metadata fields
                    new FieldMapping("FromID", "$_metadata.FromID", typeof(long)),
                    new FieldMapping("RawID", "$_metadata.RAWID", typeof(string)),
                    new FieldMapping("RawDate", "$_metadata.RAWDATE", typeof(DateTime)),
                    new FieldMapping("EffectiveFrom", "$_metadata.EffectiveFrom", typeof(DateTimeOffset)),
                    
                    // Voucher details
                    new FieldMapping("VoucherCode", "$Voucher.VoucherCode", typeof(string)),
                    new FieldMapping("VoucherName", "$Voucher.VoucherName", typeof(string)),
                    new FieldMapping("VoucherType", "$Voucher.VoucherType", typeof(string)),
                    new FieldMapping("VoucherStatus", "$Voucher.Status", typeof(string)),
                    new FieldMapping("VoucherState", "$Voucher.State", typeof(string)),
                    new FieldMapping("VoucherClubKey", "$Voucher.ClubKey", typeof(string)),
                    new FieldMapping("VoucherClubName", "$Voucher.ClubName", typeof(string)),
                    new FieldMapping("VoucherCurrency", "$Voucher.Currency", typeof(string)),
                    new FieldMapping("VoucherAmount", "$Voucher.VoucherAmount", typeof(decimal)),
                    new FieldMapping("VoucherCreateDate", "$Voucher.CreateDate", typeof(string)),
                    new FieldMapping("VoucherExpiryDate", "$Voucher.ExpiryDate", typeof(string)),
                    new FieldMapping("VoucherRedeemDate", "$Voucher.RedeemDate", typeof(DateTimeOffset)),
                    new FieldMapping("VoucherRedeemUTID", "$Voucher.RedeemUTID", typeof(string)),
                    
                    // Event details
                    new FieldMapping("EventDate", "$EventDate", typeof(DateTimeOffset)),
                    new FieldMapping("EventName", "$EventName", typeof(string)),
                    new FieldMapping("EntityName", "$EntityName", typeof(string)),
                    new FieldMapping("TriggerUTID", "$TriggerUTID", typeof(string)),
                    new FieldMapping("ReversalReason", "$ReversalReason", typeof(string)),
                    new FieldMapping("TriggerEventName", "$TriggerEventName", typeof(string)),
                    
                    // Original Card details
                    new FieldMapping("OriginalCardKey", "$OriginalCard.CardKey", typeof(string)),
                    new FieldMapping("OriginalCardClubKey", "$OriginalCard.ClubKey", typeof(string)),
                    new FieldMapping("OriginalCardType", "$OriginalCard.CardType", typeof(string)),
                    new FieldMapping("OriginalCardState", "$OriginalCard.CardState", typeof(string)),
                    new FieldMapping("OriginalCardStatus", "$OriginalCard.CardStatus", typeof(string)),
                    new FieldMapping("OriginalCardBalance", "$OriginalCard.CardBalance", typeof(decimal)),
                    new FieldMapping("OriginalCardProfileKey", "$OriginalCard.ProfileKey", typeof(string)),
                    
                    // API Body details
                    new FieldMapping("APIUTID", "$APIBody.UTID", typeof(string)),
                    new FieldMapping("APIReason", "$APIBody.Reason", typeof(string)),
                    new FieldMapping("APIEntityRef", "$APIBody.EntityRef", typeof(string)),
                    new FieldMapping("APIEntityType", "$APIBody.EntityType", typeof(string)),
                    
                    // Original Transaction details
                    new FieldMapping("OriginalEventDate", "$OriginalEventDate", typeof(DateTimeOffset)),
                    new FieldMapping("OriginalRedeemValue", "$OriginalRedeemValue", typeof(string)),
                    new FieldMapping("OriginalRedeemCurrency", "$OriginalRedeemCurrency", typeof(string)),
                    new FieldMapping("OriginalProfileKey", "$OriginalProfileKey", typeof(string))
                }
            };
        }

        private static async Task ProcessEntityType(IMongoDatabase database, string entityType, EntityTypeMapping mapping)
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Extracting {mapping.Description} from '{entityType}' collection...");

            var collection = database.GetCollection<BsonDocument>(entityType);

            // Check if collection exists and has documents
            long count = await collection.CountDocumentsAsync(new BsonDocument());
            if (count == 0)
            {
                Console.WriteLine($"No documents found in the '{entityType}' collection.");
                return;
            }

            Console.WriteLine($"Found {count:N0} documents to extract.");

            // Create MongoDB aggregation pipeline
            var projectStage = CreateProjectStage(mapping.FieldMappings);
            var pipeline = new[] { projectStage };

            // Execute the aggregation in batches
            int processedDocuments = 0;
            int totalBatches = (int)Math.Ceiling(count / (double)BatchSize);

            using (var cursor = await collection.AggregateAsync<BsonDocument>(pipeline))
            {
                // Process documents in batches to avoid memory issues
                var documents = new List<BsonDocument>();
                int currentBatch = 1;

                await cursor.ForEachAsync(document =>
                {
                    documents.Add(document);

                    if (documents.Count >= BatchSize)
                    {
                        // Process this batch
                        ProcessBatch(entityType, mapping, documents, currentBatch, totalBatches);

                        // Update progress
                        processedDocuments += documents.Count;
                        documents.Clear();
                        currentBatch++;

                        // Report progress
                        double percentComplete = (double)processedDocuments / count * 100;
                        Console.WriteLine($"Progress: {percentComplete:F2}% ({processedDocuments:N0}/{count:N0})");
                    }
                });

                // Process any remaining documents
                if (documents.Count > 0)
                {
                    ProcessBatch(entityType, mapping, documents, currentBatch, totalBatches);
                    processedDocuments += documents.Count;
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Completed extraction of {processedDocuments:N0} documents from '{entityType}' in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
        }

        private static void ProcessBatch(string entityType, EntityTypeMapping mapping, List<BsonDocument> documents, int currentBatch, int totalBatches)
        {
            Console.WriteLine($"Processing batch {currentBatch} of {totalBatches} for '{entityType}'...");

            // Convert documents to DataTable
            var dataTable = ConvertToDataTable(entityType, mapping, documents);

            // Insert into SQL
            InsertIntoSqlServer(dataTable, entityType, currentBatch == 1);
        }

        private static BsonDocument CreateProjectStage(List<FieldMapping> fieldMappings)
        {
            var projectFields = new BsonDocument();

            // Add _id: 0 to exclude MongoDB's _id field
            projectFields.Add("_id", 0);

            // Add all field mappings
            foreach (var mapping in fieldMappings)
            {
                projectFields.Add(mapping.TargetField, mapping.SourcePath);
            }

            return new BsonDocument("$project", projectFields);
        }

        private static DataTable ConvertToDataTable(string entityType, EntityTypeMapping mapping, List<BsonDocument> documents)
        {
            if (documents.Count == 0)
                return new DataTable();

            var dataTable = new DataTable(entityType);

            // Add columns to DataTable based on field mappings
            foreach (var fieldMapping in mapping.FieldMappings)
            {
                dataTable.Columns.Add(fieldMapping.TargetField, fieldMapping.TargetType);
            }

            // Add rows to DataTable
            foreach (var document in documents)
            {
                DataRow row = dataTable.NewRow();

                foreach (var fieldMapping in mapping.FieldMappings)
                {
                    string fieldName = fieldMapping.TargetField;

                    if (document.Contains(fieldName))
                    {
                        var bsonValue = document[fieldName];

                        if (bsonValue.IsBsonNull)
                        {
                            row[fieldName] = DBNull.Value;
                        }
                        else
                        {
                            try
                            {
                                object value = ConvertBsonValueToClrType(bsonValue, fieldMapping.TargetType);
                                row[fieldName] = value ?? DBNull.Value;
                            }
                            catch (Exception)
                            {
                                // If conversion fails, use null
                                row[fieldName] = DBNull.Value;
                            }
                        }
                    }
                    else
                    {
                        // Field not in document
                        row[fieldName] = DBNull.Value;
                    }
                }

                dataTable.Rows.Add(row);
            }
            return dataTable;
        }

        private static object ConvertBsonValueToClrType(BsonValue bsonValue, Type targetType)
        {
            if (bsonValue.IsBsonNull)
                return null;

            try
            {
                if (targetType == typeof(string))
                {
                    return bsonValue.ToString();
                }
                else if (targetType == typeof(int))
                {
                    return bsonValue.IsInt32 ? bsonValue.AsInt32 :
                           bsonValue.IsInt64 ? (int)bsonValue.AsInt64 :
                           bsonValue.IsDouble ? (int)bsonValue.AsDouble :
                           bsonValue.IsString ? int.TryParse(bsonValue.AsString, out int i) ? i : (object)null :
                           null;
                }
                else if (targetType == typeof(long))
                {
                    return bsonValue.IsInt64 ? bsonValue.AsInt64 :
                           bsonValue.IsInt32 ? (long)bsonValue.AsInt32 :
                           bsonValue.IsDouble ? (long)bsonValue.AsDouble :
                           bsonValue.IsString ? long.TryParse(bsonValue.AsString, out long l) ? l : (object)null :
                           null;
                }
                else if (targetType == typeof(decimal))
                {
                    return bsonValue.IsDecimal128 ? bsonValue.AsDecimal :
                           bsonValue.IsDouble ? (decimal)bsonValue.AsDouble :
                           bsonValue.IsInt32 ? (decimal)bsonValue.AsInt32 :
                           bsonValue.IsInt64 ? (decimal)bsonValue.AsInt64 :
                           bsonValue.IsString ? decimal.TryParse(bsonValue.AsString, out decimal d) ? d : (object)null :
                           null;
                }
                else if (targetType == typeof(double))
                {
                    return bsonValue.IsDouble ? bsonValue.AsDouble :
                           bsonValue.IsDecimal128 ? (double)bsonValue.AsDecimal :
                           bsonValue.IsInt32 ? (double)bsonValue.AsInt32 :
                           bsonValue.IsInt64 ? (double)bsonValue.AsInt64 :
                           bsonValue.IsString ? double.TryParse(bsonValue.AsString, out double d) ? d : (object)null :
                           null;
                }
                else if (targetType == typeof(bool))
                {
                    return bsonValue.IsBoolean ? bsonValue.AsBoolean :
                           bsonValue.IsString ? bool.TryParse(bsonValue.AsString, out bool b) ? b : (object)null :
                           null;
                }
                else if (targetType == typeof(DateTime))
                {
                    return bsonValue.IsBsonDateTime ? bsonValue.ToUniversalTime() :
                           bsonValue.IsString ? DateTime.TryParse(bsonValue.AsString, out DateTime dt) ? dt : (object)null :
                           null;
                }
                else if (targetType == typeof(DateTimeOffset))
                {
                    return bsonValue.IsBsonDateTime ? new DateTimeOffset(bsonValue.ToUniversalTime()) :
                           bsonValue.IsString ? DateTimeOffset.TryParse(bsonValue.AsString, out DateTimeOffset dto) ? dto : (object)null :
                           null;
                }
                else if (targetType == typeof(Guid))
                {
                    return bsonValue.IsString ? Guid.TryParse(bsonValue.AsString, out Guid g) ? g : (object)null :
                           null;
                }
                else
                {
                    // For other types, convert to string
                    return bsonValue.ToString();
                }
            }
            catch
            {
                return null;
            }
        }

        private static void InsertIntoSqlServer(DataTable dataTable, string tableName, bool isFirstBatch)
        {
            if (dataTable.Rows.Count == 0)
                return;

            try
            {
                using (var connection = new SqlConnection(SqlConnectionString))
                {
                    connection.Open();

                    // Check if table exists
                    bool tableExists = TableExists(connection, tableName);

                    // Create table if it doesn't exist
                    if (!tableExists)
                    {
                        CreateSqlTable(connection, dataTable, tableName);
                        Console.WriteLine($"Created SQL table '{tableName}'.");
                    }
                    else if (isFirstBatch && TruncateExistingTables)
                    {
                        // Truncate table if requested and this is the first batch
                        TruncateSqlTable(connection, tableName);
                        Console.WriteLine($"Truncated SQL table '{tableName}'.");
                    }

                    // Insert data
                    using (var bulkCopy = new SqlBulkCopy(connection))
                    {
                        bulkCopy.DestinationTableName = tableName;
                        bulkCopy.BatchSize = BatchSize;
                        bulkCopy.BulkCopyTimeout = 600; // 10 minutes

                        // Set up column mappings
                        foreach (DataColumn column in dataTable.Columns)
                        {
                            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                        }

                        // Insert data
                        bulkCopy.WriteToServer(dataTable);
                    }

                    Console.WriteLine($"Inserted {dataTable.Rows.Count:N0} rows into '{tableName}'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting into SQL Server: {ex.Message}");
                throw;
            }
        }

        private static bool TableExists(SqlConnection connection, string tableName)
        {
            using (var command = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name = @TableName", connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                return (int)command.ExecuteScalar() > 0;
            }
        }

        private static void CreateSqlTable(SqlConnection connection, DataTable schema, string tableName)
        {
            // Build CREATE TABLE SQL statement
            var sql = new System.Text.StringBuilder();
            sql.AppendLine($"CREATE TABLE {tableName} (");
            sql.AppendLine("  [Id] INT IDENTITY(1,1) PRIMARY KEY,");

            for (int i = 0; i < schema.Columns.Count; i++)
            {
                DataColumn column = schema.Columns[i];
                string sqlType = GetSqlTypeFromClrType(column.DataType);

                sql.Append($"  [{column.ColumnName}] {sqlType}");

                if (i < schema.Columns.Count - 1)
                    sql.AppendLine(",");
                else
                   
                    sql.AppendLine();
            }

            sql.AppendLine(")");

            // Execute CREATE TABLE statement
            using (var command = new SqlCommand(sql.ToString(), connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private static void TruncateSqlTable(SqlConnection connection, string tableName)
        {
            using (var command = new SqlCommand($"TRUNCATE TABLE {tableName}", connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private static string GetSqlTypeFromClrType(Type clrType)
        {
            if (clrType == typeof(string))
                return "NVARCHAR(MAX)";
            else if (clrType == typeof(int))
                return "INT";
            else if (clrType == typeof(long))
                return "BIGINT";
            else if (clrType == typeof(decimal))
                return "DECIMAL(18, 2)";
            else if (clrType == typeof(double))
                return "FLOAT";
            else if (clrType == typeof(bool))
                return "BIT";
            else if (clrType == typeof(DateTime))
                return "DATETIME2";
            else if (clrType == typeof(DateTimeOffset))
                return "DATETIMEOFFSET";
            else if (clrType == typeof(Guid))
                return "UNIQUEIDENTIFIER";
            else if (clrType == typeof(byte[]))
                return "VARBINARY(MAX)";
            else
                return "NVARCHAR(MAX)";
        }
    }

    /// <summary>
    /// Defines the mapping for an entity type
    /// </summary>
    class EntityTypeMapping
    {
        public string Description { get; set; }
        public List<FieldMapping> FieldMappings { get; set; }
    }

    /// <summary>
    /// Defines the mapping for a single field
    /// </summary>
    class FieldMapping
    {
        public string TargetField { get; private set; }
        public string SourcePath { get; private set; }
        public Type TargetType { get; private set; }

        public FieldMapping(string targetField, string sourcePath, Type targetType)
        {
            TargetField = targetField;
            SourcePath = sourcePath;
            TargetType = targetType;
        }
    }
}