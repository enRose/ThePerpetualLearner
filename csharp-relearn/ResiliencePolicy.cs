using Polly;
using Polly.Timeout;
using System;
using System.Collections.Concurrent;

namespace ASB.API.TrueRewardsRedemptionExp
{
    public enum PolicyType
    {
        Retry,

        CircuitBreaker,

        PessimisticTimeout
    }

    public class PolicyStore
    {
        private readonly ConcurrentDictionary<PolicyType, Policy> store;

        public PolicyStore()
        {
            store = new ConcurrentDictionary<PolicyType, Policy>();

            store.TryAdd(PolicyType.Retry, new RetryPolicy().Get());

            store.TryAdd(PolicyType.CircuitBreaker, new CircuitBreaker().Get());

            store.TryAdd(PolicyType.PessimisticTimeout, new PessimisticTimeout().Get());
        }

        public Policy Get(PolicyType type)
        {
            if (store.TryGetValue(type, out Policy policy))
            {
                return policy;
            }

            return null;
        }
    }

    public class RetryPolicy
    {
        private int maxRetryAttempts = 3;

        private bool useExponentialBackoff = true;

        private TimeSpan pauseInMilliSecBetweenFailures = TimeSpan.FromMilliseconds(200);

        private Action<Exception, TimeSpan, int, Context> onRetry =
                (exception, timeSpan, retryCount, context) => { };

        private Func<int, TimeSpan> sleepDurationProvider;

        public Policy Get()
        {
            sleepDurationProvider = retryCount => pauseInMilliSecBetweenFailures;

            if (useExponentialBackoff == true)
            {
                sleepDurationProvider = retryCount => 
                    TimeSpan.FromMilliseconds(
                        Math.Pow(200, retryCount)
                    );
            }

            return Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(

                    maxRetryAttempts,

                    sleepDurationProvider,

                    onRetry
                );
        }
    }

    public class CircuitBreaker
    {
        private int exceptionsAllowedBeforeBreaking = 2;

        private TimeSpan durationOfBreak = TimeSpan.FromMinutes(1);

        private Action<Exception, TimeSpan> onBreak = (exception, timespan) => { };

        public static Action onReset = () => { };

        public Policy Get()
        {
            return Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking,

                    durationOfBreak,

                    onBreak,

                    onReset
                );
        }
    }

    public class PessimisticTimeout
    {
        private int waitInSeconds = 7;

        public Policy Get()
        {
            return Policy.Timeout(waitInSeconds, TimeoutStrategy.Pessimistic);
        }
    }
}