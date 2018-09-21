
## methods should be small

## methods should do one thing and do it well

## Avoid long argument list

Anything over 3 arguments long is too much. 

Consider wrap long argument list into an object.

```
public async Task<IEnumerable<ToyDto>> GetAllByAsync(
    Guid toyId, 
    int? channel, 
    int? commsReason, 
    DateTime? timeframeStart, 
    DateTime? timeframeEnd, 
    string campaignCode, 
    string campaignName, 
    int? sourceApplication,
    DateTime? contentValidStartDate, 
    DateTime? contentValidEndDate, 
    string productItemCode, 
    DateTime? lastUpdateDate)
{
    //
}

public class ToySearchCriteriaDto 
{
    public Guid toyId { get; set; } 
    public int? channel { get; set; } 
    public int? commsReason { get; set; } 
    public DateTime? timeframeStart { get; set; } 
    public DateTime? timeframeEnd { get; set; } 
    public string campaignCode { get; set; } 
    public string campaignName { get; set; } 
    public int? sourceApplication { get; set; }
    public DateTime? contentValidStartDate { get; set; } 
    public DateTime? contentValidEndDate { get; set; } 
    public string productItemCode { get; set; } 
    public DateTime? lastUpdateDate { get; set; }
}

public async Task<IEnumerable<ToyDto>> GetAllByAsync(
    ToySearchCriteriaDto searchCriteria)
{

}
```

## Monadic methods

1. Use argument name to form a readable sentence 

    GetCreditCardAccountsFrom(allAccounts)

2. Consider convert long argument list methods into monadic

    GetAllByAsync(ToySearchCriteriaDto searchCriteria)