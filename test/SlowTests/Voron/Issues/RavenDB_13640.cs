﻿using System;
using FastTests.Voron;
using Voron;
using Xunit;

namespace SlowTests.Voron.Issues
{
    public class RavenDB_13640 : StorageTest
    {
        protected override void Configure(StorageEnvironmentOptions options)
        {
            options.ManualFlushing = true;
        }

        [Fact]
        public void Should_properly_clear_transaction_after_begin_async_commit_failure()
        {
            var tx1 = Env.WriteTransaction();

            try
            {
                var testingStuff = tx1.LowLevelTransaction.ForTestingPurposesOnly();

                using (testingStuff.CallDuringBeginAsyncCommitAndStartNewTransaction(() => throw new InvalidOperationException()))
                {
                    tx1.LowLevelTransaction.BeforeCommitFinalization += delegate { throw  new InvalidOperationException();};

                    Assert.Throws<InvalidOperationException>(() => tx1.BeginAsyncCommitAndStartNewTransaction());
                }
            }
            finally
            {
                tx1?.Dispose();
            }

            // the issue was that the following was not released:
            // StorageEnvironment._currentWriteTransactionHolder
            // StorageEnvironment._writeTransactionRunning
            // StorageEnvironment._transactionWriter
            // during StorageEnvironment.TransactionCompleted on tx dispose because the transaction was still
            // recognized as having AsyncCommit task in progress while we failed on BeginAsyncCommitAndStartNewTransaction

            using (var tx = Env.WriteTransaction())
            {
                tx.LowLevelTransaction.ModifyPage(0);
                tx.Commit();
            }
        }
    }
}