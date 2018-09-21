"[comments] are a sweet smell…
[unfortunately they are] often used as a
deodorant (until they get out of date)."

Martin Fowler

## why we commet ?
* Explains the why, NOT the what or how

* servers as a warning
    ```
    // please don't change this regex.
    // It has been through numerous iterations and discussion.
    // We settled on this regex to match what customer service does.
    // see:
    // https://davidcel.is/posts/stop-validating-email-with-regex/ 
    const emailValidationRegex = '@';
    ```

* Documenting a hack or issue
    ```
    // when credit card in credit, balance goes negative
    var makeItZeroIfCreditCardInCredit = Math.Max(0, takeSmallerAmount);
    ```
* documenting an unusual or uncommon usecase 

## bad comment

* commenting-out code
```
catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
{
    throw new NotFoundException("CIF Not Found for Supplied Token");
    
    // throw new ValidationErrorException("CIF Not Found for Supplied Token", nameof(CustomerViewDto.Id), "Not Found", 44);
}
```

* commenting the what
    ```
    //Try to get token for the PAN from CardToken database
    token = await _repository.GetTokenAsync(pan).ConfigureAwait(false);          

    //Token found on the DB, so send it back to controller
    if (!string.IsNullOrEmpty(token))
        return token;                                             

    //Get the prefix for given bin from DB 
    var cardBin = Convert.ToInt32(pan.Substring(0, 6));
    TOKEN_PREFIX = await _repository.GetTokenPrefixNumber(cardBin).ConfigureAwait(false);               

    //Last digit of PAN is the check digit
    var checkDigit = pan.Substring(15, 1);

    //Last 4 digits of PAN
    var suffix = pan.Substring(12, 4);                            

    //Get a list of free tokens for the given bin from DB
    var freeTokens = new HashSet<int>();
    freeTokens = await _repository.GetFreeTokens(cardBin).ConfigureAwait(false);                        
    foreach (var freeToken in freeTokens)
    {
        var insertFreeTokenResult = new InsertTokenResult 
        {
             TokenUsedFlag = true, 
             TokenBaseNumb = freeToken 
        };

        //Create a new token
        token = string.Concat(TOKEN_PREFIX, (freeToken).ToString().PadLeft(9, '0'), suffix);            

        //Do mod 10 check, if valid attempt to commit to DB
        if (Mod10(token) == checkDigit)
        {
            //Attempt to commit to DB
            insertFreeTokenResult = await _repository.TryInsertTokenAsync(
                    token, freeToken, pan, TOKEN_PREFIX
                )
                .ConfigureAwait(false);

            //Commit succesful, token was not already in use
            if (!insertFreeTokenResult.TokenUsedFlag)
            {
                //Remove the free token off list of tokens
                await _repository.DeleteFreeBaseNumber(cardBin, freeToken).ConfigureAwait(false);       

                //Debug.WriteLine(insertFreeTokenResult.TokenUsedFlag + " From Free:" + token);
                
                //Return comitted token back to controller
                return token;                        
            }
        }
    }
    ```

    ```
    token = await _repository
    .TryGetTokenFromDBFor(pan)
    .ConfigureAwait (false);

    if (!string.IsNullOrEmpty (token)) 
    {
        return token;
    }

    var cardBin = pan.GetCardBin();

    TOKEN_PREFIX = await _repository
        .TryGetTokenPrefixFromDBFor(cardBin)
        .ConfigureAwait (false);

    var checkDigit = pan.GetCheckDigit();
    var suffix = pan.GetSuffix();

    var freeTokens = await _repository
        .GetFreeTokensFor(cardBin)
        .ConfigureAwait(false);

    foreach (var freeToken in freeTokens) {
        
        var newToken = CreateNewTokenFrom(TOKEN_PREFIX, freeToken, suffix);

        var isValid = CheckMod10For(newToken, checkDigit);

        if (isValid) {
            
            var result = await 
                TryCommitToDBWith(
                    new NewTokenInfoCommitToDB 
                    {
                        newToken = newToken,
                        freeToken = freeToken, 
                        pan = pan, 
                        prefix = TOKEN_PREFIX
                    })
                .ConfigureAwait (false);

            var isTokenAlreadyInUse = result.TokenUsedFlag;

            if (isTokenAlreadyInUse == false) 
            {    
                await TryMarkStatusAsTakenFor(
                    freeToken,
                    cardBin
                    )
                .ConfigureAwait (false);

                return newToken;
            }
        }
    }

    private async Task<bool> TryMarkStatusAsTakenFor(int thisFreeToken, string cardBin) 
    {
        return await _repository.DeleteFreeBaseNumber(
                cardBin, 
                thisFreeToken
            )
            .ConfigureAwait(false);
    }

    internal class NewTokenInfoCommitToDB
    {
        string newToken {get;set;}
        int freeToken {get;set;}
        string pan {get;set;}
        int prefix {get;set;}
    } 

    private async Task<InsertTokenResult> TryCommitToDBWith(
        NewTokenInfoCommitToDB newTokenInfo) 
    {
        return await _repository.TryInsertTokenAsync(
            newTokenInfo.newToken, 
            newTokenInfo.freeToken, 
            newTokenInfo.pan, 
            newTokenInfo.prefix
        )
        .ConfigureAwait(false);
    }

    private bool CheckMod10For(string token, string checkDigit)
    {
        return Mod10(token) == checkDigit;
    }

    private string CreateNewTokenFrom(int prefix, int freeToken, string suffix) 
    {
        return string.Concat(
            prefix,

            freeToken.ToString().PadLeft(9, '0'), 
            
            suffix
        );
    }

    private async Task<string> GetTokenPrefixFromDBFor(string pan)
    {
        var cardBin = Convert.ToInt(pan.Substring (0, 6));

        return await _repository
            .GetTokenPrefixNumber(cardBin)
            .ConfigureAwait (false);
    }


    public static class PanExtensions
    {
        public static int GetCardBin(this string pan)
        {
            return Convert.ToInt(pan.Substring(0, 6));
        }

        public static string GetCheckDigit(this string pan)
        {
            return pan.Substring(pan.Length - 1);
        }

        public static string GetSuffix(this string pan)
        {
            return pan.Substring(pan.Length - 4);
        }
    }

    // facade pattern
    public class Repository {
        
        public async Task<string> TryGetTokenFromDBFor (string pan) {
            await this.GetTokenAsync(pan).ConfigureAwait (false);
        }

        public async Task<string> TryGetTokenPrefixFromDBFor(string cardBin) {
            await this.TryGetTokenPrefixNumber(cardBin).ConfigureAwait (false);
        }
    }
    ```

* For the sake of tooling
    ```
    /// <summary>
    /// Get a list of toys for given customer
    /// </summary>
    /// <param name="customerId">Tokenised Customer CIF Number</param>
    /// <param name="channel">Optional toys Delivery Channel</param>
    /// <param name="timeframeBegins">Optional start of timeframe the message was delivered</param>
    /// <param name="timeframeEnds">Optional end of timeframe the message was delivered</param>
    /// <param name="commsReason">Optional filter by message type</param>
    /// <param name="promotionCode">Optional filter by campaign code</param>
    /// <param name="promotionName">Optional filter campaign name</param>
    /// <param name="clientApplication">Optional filter source application</param>
    /// <param name="contentValidStartDate">Optional filter on records got ContentValidStartDttm equal or greater than this date</param>
    /// <param name="contentValidEndDate">Optional filter on records got ContentValidEndDttm equal or less than this date</param>
    /// <param name="productCode">Optional filter product item code e.g. account number</param>
    /// <param name="lastUpdateDate">Optional filter only for experience API Cache to filter out the latest data</param>
    /// <returns>List of toys</returns>
    [SwaggerResponse(HttpStatusCode.OK, type: typeof(IEnumerable<CommunicationViewDto>))]
    [Route("~/v1/customers/{customerId:guid}/toys", Name = nameof(GetAllToys))]
    [HttpGet]
    public async Task<IHttpActionResult> GetAllToys(Guid customerId, [FromUri]int? channel = null, 
        [FromUri]int? commsReason = null, [FromUri]DateTime? timeframeBegins = null, [FromUri]DateTime? timeframeEnds = null,
        [FromUri]string promotionCode = null, [FromUri]string promotionName = null, [FromUri]int? clientApplication = null,
        [FromUri]DateTime? contentValidStartDate = null, [FromUri]DateTime? contentValidEndDate = null, [FromUri]string productCode = null,
        [FromUri]DateTime? lastUpdateDate = null)
    {
        var items = await _service.GetAllByCustomerAsync(customerId, channel, commsReason, timeframeBegins, timeframeEnds,
            promotionCode, promotionName, clientApplication, contentValidStartDate, contentValidEndDate, productCode, lastUpdateDate);
        return Ok(items);
    }
    ```
    
    ```
    which one to trust ? variable name vs comment
    <param name="commsReason">Optional filter by message type</param>
    ```

* Commenting how
    ```
    /* run it through business rule first,
       if credit card is closed, we compare
       credit card current balance with transferable amount,
       make sure transfer amount not exceed current balance,

       if current balance is negative, it meams credit card is in credit,

       we then set transferable amount to zero. 
    */
    var shouldLimit = new CreditCardAcctHasValidClosedStatusRule()
        .Check(
        new TxnRuleCtx
        {
            CreditCardAccount = creditCardAccount
        });

    if (shouldLimit)
    {
        txn.TransferableAmount = Math.Max(0, Math.Min(txn.TransferableAmount, creditCardAccount.CurrentBalance));
    }
    ```

    trouble is some time we are too clever and try to condense our intent

    rather being clever, let's try KISS.

    Basically when we read it out, it is effectively the 'how' we describe in comment.
    ```
    var isCreditCardClosed = new CreditCardAcctHasValidClosedStatusRul()
    .Check(new TxnRuleCtx
    {
        CreditCardAccount = creditCardAccount
    });

    if (isCreditCardClosed)
    {
        var isCreditCardBalanceInNegative = creditCardAccount.CurrentBalance < 0;

        var isCreditCardInCredit = isCreditCardBalanceInNegative == true;

        if (isCreditCardInCredit)
        {
            txn.TransferableAmount = 0;
        }
        else if(txn.TransferableAmount > creditCardAccount.CurrentBalance)
        {
            txn.TransferableAmount = creditCardAccount.CurrentBalance;
        }
        
    }
    ```

* Essays

* Commenting the gentleman's agreement

    what is gentleman's agreement?

    Bob: 
    
    Alice, if I say 'v1/cases/actions/UpdateNameOnCard?newName="Joe"&delete=fasle', it means I want to update my name on card. 
    
    But if I say 'v1/cases/actions/UpdateNameOnCard?newName=""&delete=true', it means I want to set my name on card to my default name.

    how we end up with gentleman's agreement?
    * convenient to code this way
    * This is how downstream systems work so as upstream clients
    * for reuseability 

    why gentleman's agreement is not ideal ?
    * not very intent revealing
    * it usually indicates the violation of single responsibility
    * it is often semantically incorrect which mayb lead to unreadability and confusion
    * if it mirrors how downstream systems are implemented, then changes in downstream could potentially affect levels up

    How to break gentleman's agreement:
    * use adapter pattern to decouple upstream implementation from downstream
    * separate semantically different functionalities into multiple dedicated computation units, i.e. method, class, etc.
    * extract those truely shared computation units if there are any

