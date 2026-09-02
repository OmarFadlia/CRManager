using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CRManager.Shared.DTOs;

namespace CRManager.Shared.Services;

public class CardApiService(HttpClient httpClient) : ICardApiService
{
    public async Task<List<CreditCardSummaryDto>> GetCardsAsync()
    {
        try
        {
            var cards = await httpClient.GetFromJsonAsync<List<CreditCardSummaryDto>>("api/creditcards");
            if (cards != null)
                return cards;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CardApiService] GetCardsAsync error: {ex.Message}");
        }

        return new List<CreditCardSummaryDto>();
    }

    public async Task<CreditCardSummaryDto?> GetCardByIdAsync(Guid id)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<CreditCardSummaryDto>($"api/creditcards/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CardApiService] GetCardByIdAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<DashboardSummaryDto?> GetDashboardSummaryAsync()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<DashboardSummaryDto>("api/creditcards/dashboard");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CardApiService] GetDashboardSummaryAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<CreditCardSummaryDto?> CreateCardAsync(CreateCreditCardRequest request)
    {
        try
        {
            var res = await httpClient.PostAsJsonAsync("api/creditcards", request);
            if (res.IsSuccessStatusCode)
            {
                return await res.Content.ReadFromJsonAsync<CreditCardSummaryDto>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CardApiService] CreateCardAsync error: {ex.Message}");
        }

        return null;
    }

    public async Task<CreditCardSummaryDto?> UpdateCardAsync(Guid id, UpdateCreditCardRequest request)
    {
        try
        {
            var res = await httpClient.PutAsJsonAsync($"api/creditcards/{id}", request);
            if (res.IsSuccessStatusCode)
            {
                return await res.Content.ReadFromJsonAsync<CreditCardSummaryDto>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CardApiService] UpdateCardAsync error: {ex.Message}");
        }

        return null;
    }

    public async Task<bool> DeleteCardAsync(Guid id)
    {
        try
        {
            var res = await httpClient.DeleteAsync($"api/creditcards/{id}");
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CardApiService] DeleteCardAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(Guid? cardId = null)
    {
        try
        {
            var url = cardId.HasValue && cardId.Value != Guid.Empty ? $"api/transactions?cardId={cardId.Value}" : "api/transactions";
            var list = await httpClient.GetFromJsonAsync<List<TransactionDto>>(url);
            if (list != null)
                return list;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CardApiService] GetTransactionsAsync error: {ex.Message}");
        }

        return new List<TransactionDto>();
    }

    public async Task<TransactionDto?> LogTransactionAsync(CreateTransactionRequest request)
    {
        try
        {
            var res = await httpClient.PostAsJsonAsync("api/transactions", request);
            if (res.IsSuccessStatusCode)
            {
                return await res.Content.ReadFromJsonAsync<TransactionDto>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CardApiService] LogTransactionAsync error: {ex.Message}");
        }

        return null;
    }

    public async Task<TransactionDto?> ToggleTransactionPaidAsync(Guid id)
    {
        try
        {
            var res = await httpClient.PutAsync($"api/transactions/{id}/toggle-paid", null);
            if (res.IsSuccessStatusCode)
            {
                return await res.Content.ReadFromJsonAsync<TransactionDto>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CardApiService] ToggleTransactionPaidAsync error: {ex.Message}");
        }

        return null;
    }

    public async Task<bool> DeleteTransactionAsync(Guid id)
    {
        try
        {
            var res = await httpClient.DeleteAsync($"api/transactions/{id}");
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CardApiService] DeleteTransactionAsync error: {ex.Message}");
            return false;
        }
    }
}
