
token = await _repository.TryGetTokenFromDBFor(pan).ConfigureAwait (false);

if (!string.IsNullOrEmpty (token)) 
{
    return token;
}

var cardBin = pan.GetCardBin();

TOKEN_PREFIX = await _repository.TryGetTokenPrefixFromDBFor(cardBin).ConfigureAwait (false);

var checkDigit = pan.GetCheckDigit();
var suffix = pan.GetSuffix();

var freeTokens = await _repository.GetFreeTokensFor(cardBin).ConfigureAwait(false);

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

