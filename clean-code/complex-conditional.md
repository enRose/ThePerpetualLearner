
## too many conditions



## too obscure

Even though we have only two conditions, but it is not very obvious what we try to guard against.

```
if (
    updateAddressResponse.request.COMMAND.Count() > 0 &&
    updateAddressResponse.request.COMMAND[0] is COMMANDUpdateArrangementLinkedCardsContactPreferencesResponse
    )
{
    //
}
```

If we have spent some time to understand it, then write out using a variable name

```
var addressUpdateResultFromRCU = 
updateAddressResponse.request.COMMAND.Count() > 0 &&
    updateAddressResponse.request.COMMAND[0] is COMMANDUpdateArrangementLinkedCardsContactPreferencesResponse

if (addressUpdateResultFromRCU == true) 
{
    //
}

```