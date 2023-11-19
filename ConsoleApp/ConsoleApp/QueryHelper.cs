using ConsoleApp.Model;
using ConsoleApp.Model.Enum;
using ConsoleApp.OutputTypes;using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleApp.Model;
using System.Text.Json;
using System.IO;

namespace ConsoleApp;

public class QueryHelper : IQueryHelper
{
    /// <summary>
    /// Get Deliveries that has payed
    /// </summary>
    public IEnumerable<Delivery> Paid(IEnumerable<Delivery> deliveries)
    {
        return deliveries.Where(delivery => delivery.PaymentId != null);
    }
    /// <summary>
    /// Get Deliveries that now processing by system (not Canceled or Done)
    /// </summary>
    public IEnumerable<Delivery> NotFinished(IEnumerable<Delivery> deliveries)
    {
        return deliveries.Where(delivery =>
            delivery.Status != DeliveryStatus.Cancelled &&
            delivery.Status != DeliveryStatus.Done
        );
    }

    /// <summary>
    /// Get DeliveriesShortInfo from deliveries of specified client
    /// </summary>
    public IEnumerable<DeliveryShortInfo> DeliveryInfosByClient(IEnumerable<Delivery> deliveries, string clientId)
    {
        var clientDeliveries = deliveries.Where(delivery => delivery.ClientId == clientId);

        return clientDeliveries.Select(delivery => new DeliveryShortInfo
        {
            Id = delivery.Id,
            StartCity = delivery.Direction.Origin.City,
            EndCity = delivery.Direction.Destination.City,
            ClientId = delivery.ClientId,
            Type = delivery.Type,
            LoadingPeriod = delivery.LoadingPeriod,
            ArrivalPeriod = delivery.ArrivalPeriod,
            Status = delivery.Status,
            CargoType = delivery.CargoType
        });
    }
    /// <summary>
    /// Get first ten Deliveries that starts at specified city and have specified type
    /// </summary>
    public IEnumerable<Delivery> DeliveriesByCityAndType(IEnumerable<Delivery> deliveries, string city, DeliveryType deliveryType)
    {
        return deliveries
            .Where(delivery => delivery.Direction.Origin.City == city && delivery.Type == deliveryType);
    }
    /// <summary>
    /// Order deliveries by status, then by start of loading period
    /// </summary>
    public IEnumerable<Delivery> OrderByStatusThenByStartLoading(IEnumerable<Delivery> deliveries)
    {
        return deliveries
            .OrderBy(delivery => delivery.Status)
            .ThenBy(delivery => delivery.LoadingPeriod.Start);
    }
    /// <summary>
    /// Count unique cargo types
    /// </summary>
    public int CountUniqCargoTypes(IEnumerable<Delivery> deliveries)
    {
        var uniqueCargoTypes = deliveries.Select(delivery => delivery.CargoType).Distinct();
        return uniqueCargoTypes.Count();
    }
    /// <summary>
    /// Group deliveries by status and count deliveries in each group
    /// </summary>
    public Dictionary<DeliveryStatus, int> CountsByDeliveryStatus(IEnumerable<Delivery> deliveries)
    {
        var groupedDeliveries = deliveries.GroupBy(delivery => delivery.Status)
                                          .ToDictionary(group => group.Key, group => group.Count());

        return groupedDeliveries;
    }
    /// <summary>
    /// Group deliveries by start-end city pairs and calculate average gap between end of loading period and start of arrival period (calculate in minutes)
    /// </summary>
    public IEnumerable<AverageGapsInfo> AverageTravelTimePerDirection(IEnumerable<Delivery> deliveries)
    {
        var averageGapsInfo = deliveries
            .GroupBy(
                delivery => new { StartCity = delivery.Direction.Origin.City, EndCity = delivery.Direction.Destination.City },
                (key, group) => new AverageGapsInfo
                {
                    StartCity = key.StartCity,
                    EndCity = key.EndCity,
                    AverageGap = group.Average(delivery =>
                    {
                        if (delivery.LoadingPeriod.End.HasValue && delivery.ArrivalPeriod.Start.HasValue)
                        {
                            return (delivery.ArrivalPeriod.End.Value - delivery.LoadingPeriod.Start.Value).TotalMinutes;
                        }
                        return 0; // Повернення нуля у випадку null значень
                    })
                })
            .OrderBy(info => info.StartCity)
            .ThenBy(info => info.EndCity)
            .ToList();

        return averageGapsInfo;
    }
    /// <summary>
    /// Paging helper
    /// </summary>
    public IEnumerable<TElement> Paging<TElement, TOrderingKey>(
        IEnumerable<TElement> elements,
        Func<TElement, TOrderingKey> ordering,
        Func<TElement, bool>? filter = null,
        int countOnPage = 100,
        int pageNumber = 1)
    {
        // Фільтрація
        if (filter != null)
        {
            elements = elements.Where(filter);
        }

        // Сортування
        elements = elements.OrderBy(ordering);

        // Пагінація
        return elements.Skip((pageNumber - 1) * countOnPage).Take(countOnPage);
    }
}
