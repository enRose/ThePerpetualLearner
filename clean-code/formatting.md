
## give it some space

Please tell me what below code does in 1 hour.

```
if (product == null) throw new APIException(ApiErrorCode.ResourceNotFound);
if (!product.IsValidNominatedProductForVisaDebit.GetValueOrDefault()) throw new APIException(ApiErrorCode.EditAccountMethodNotImplementedForThisResource);
// Initialise account stem
accountStem = account.AccountNumber.Substring(0, 13);
// The FAS 160217 call is encapsulated in the Edit Account Provider
COMMANDUpdateArrangementLinkedCardsContactPreferencesResponse fas160217Response = null;
var updateAddressResponse = _editAccountProvider.UpdateAccountAddress(request.Address, account.AccountNumber);
if (updateAddressResponse != null && updateAddressResponse.request != null &&
        updateAddressResponse.request.COMMAND != null && updateAddressResponse.request.COMMAND.Count() > 0 &&
        updateAddressResponse.request.COMMAND[0] is COMMANDUpdateArrangementLinkedCardsContactPreferencesResponse)
{
    fas160217Response = updateAddressResponse.request.COMMAND[0] as COMMANDUpdateArrangementLinkedCardsContactPreferencesResponse;
}
else
{
    throw new APIException(ApiErrorCode.UnmatchedBusinessError);
}
if (fas160217Response != null &&
            fas160217Response.errorInfo != null &&
            fas160217Response.errorInfo.All(e => e.severity == Severity.Success))
{
    isUpdated = true;
    Factory.WebOperationContextHelper.SetHttpStatusCode(System.Net.HttpStatusCode.OK);
}
else
{
    isUpdated = false;


    if (fas160217Response != null && fas160217Response.errorInfo != null)
    {
        if (fas160217Response.errorInfo.Any(x => x.productSystem.codeSpecified && x.productSystem.code == 43313 && x.severity == Severity.Warning))
        {
            throw new APIException(ApiErrorCode.InvalidFieldData, request.GetMemberJsonPath(x => x.Address.AddressLine1));
        }
        
        throw new APIException(fas160217Response.errorInfo.ToCommonErrorInfoArray().ToList());
    }
    else
    {
        throw new APIException(ApiErrorCode.UnmatchedBusinessError);
    }
}
// FAS 160217 does not notify the customer when the account address update succeeds
if (isUpdated)
{
    try
    {
        var communicationRequest = ToNotify1407(request, customerID, account);
        Factory.CommunicationEgine1407.Execute(
            x => x.NotifyCustomer(communicationRequest));
    }
    catch (Exception ex)
    {
        _loggingProvider.LogException(new APIException(ex.Message));
    }
}
```