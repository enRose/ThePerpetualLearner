
## class should be small


## Only one reason to change

Immedidately it suggests this class may do too much
```
class CustomerManager 
{
    // ...
}
```

Things to consider:

1. One reason to change  

2. cohesive - all members within a class should be working together toward a common goal

3. similar change frequency - all members of a class should change at the same rate 

4. Shouldn't have feature envy - one class is constantly using public fields of another class to perform a task

```
class CustomerFetcher 
{
}

class CustomerCifValidator
{

}

class CustomerAddressValidator
{

}
```


## avoid message chain - don't talk to stranger

```
var postCode = account.transactions[0].payee.branch.address.postCode
```

the problem with the code is if anything between account and postCode was to change, we may end up an null object error. Say change from getting payee's branch address to billing address.  

if we just want to access a property, we could break the chain into multiple class:

1. each class should be responsible for handling null

2. if downstream classes decided to change how to retrieve postCode, change should not be rippled upstream clients 

```
var postCode = account.GetPayeePostCodeFor(int txnIndex);

public string GetPayeePostCodeFor(int txnIndex)
{
    var txn = account.GetTxn(txnIndex);

    return txn?.GetPayeePostCode();
}

class Txn
{
    private Payee payee; 

    public string GetPayeePostCode()
    {
        return payee?.GetPostCode();
    }
}

class Payee
{
    private Address branchAddress;
    private Address billingAddress;

    public string GetPostCode() 
    {
        return branchAddress?.postCode;
    }
}
```

But surely, we should ask ourselves, why do we need postCode and what do we do with it after getting it? Do we use it for calculating delivery distance? Do we use it for calculating delivery cost?

If so, should we be encapsulating this computation right down to the Payee class?

```
var payee = account.GetPayeeBy(int txnIndex);

var deliveryCost = payee.CalcDelieryCost();
```
