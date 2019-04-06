using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using AsyncIO;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Store;
using Libplanet.Tx;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Serilog;
using Serilog;
using UnityEngine;
using Uno.Extensions;

namespace Nekoyume.Action
{
    public class Agent : IDisposable
    {
        internal readonly BlockChain<PolymorphicAction<ActionBase>> blocks;
        private readonly PrivateKey privateKey;
        public readonly ConcurrentQueue<PolymorphicAction<ActionBase>> queuedActions;

        private const float AvatarUpdateInterval = 3.0f;

        private const float ShopUpdateInterval = 3.0f;

        private const float TxProcessInterval = 3.0f;
        private static readonly TimeSpan SwarmDialTimeout = TimeSpan.FromMilliseconds(1000);

        private readonly Swarm swarm;

        private const int rewardAmount = 1;

        static Agent() 
        {
            ForceDotNet.Force();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(new UnityDebugSink())
                .CreateLogger();
        }

        public Agent(
            PrivateKey privateKey,
            string path,
            Guid chainId,
            IEnumerable<Peer> peers,
            IEnumerable<IceServer> iceServers,
            string host,
            int? port)
        {
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = new BlockPolicy<PolymorphicAction<ActionBase>>(TimeSpan.FromMilliseconds(500));
# if UNITY_EDITOR
            policy = new DebugPolicy();
#endif
            this.privateKey = privateKey;
            blocks = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                new FileStore(path),
                chainId);
            queuedActions = new ConcurrentQueue<PolymorphicAction<ActionBase>>();
#if BLOCK_LOG_USE
            FileHelper.WriteAllText("Block.log", "");
#endif

            swarm = new Swarm(
                privateKey,
                appProtocolVersion: 1,
                dialTimeout: SwarmDialTimeout,
                host: host,
                listenPort: port,
                iceServers: iceServers);

            foreach (var peer in peers)
            {
                swarm.Add(peer);
            }
        }

        public Address UserAddress => privateKey.PublicKey.ToAddress();
        public Address ShopAddress => ActionManager.shopAddress;

        public event EventHandler<Context> DidReceiveAction;
        public event EventHandler<Shop> UpdateShop;

        public IEnumerator CoSwarmRunner()
        {
            Task task = Task.Run(async () => { await swarm.StartAsync(blocks); });

            yield return new WaitUntil(() => task.IsCompleted);
        }

        public IEnumerator CoAvatarUpdator()
        {
            while (true)
            {
                yield return new WaitForSeconds(AvatarUpdateInterval);
                var task = Task.Run(() => blocks.GetStates(new[] {UserAddress}));
                yield return new WaitUntil(() => task.IsCompleted);
                var ctx = (Context) task.Result.GetValueOrDefault(UserAddress);
                if (ctx?.avatar != null)
                {
                    DidReceiveAction?.Invoke(this, ctx);
                }

                yield return null;
            }
        }

        public IEnumerator CoShopUpdator()
        {
            while (true)
            {
                yield return new WaitForSeconds(ShopUpdateInterval);
                var task = Task.Run(() => blocks.GetStates(new[] {ShopAddress}));
                yield return new WaitUntil(() => task.IsCompleted);
                var shop = (Shop) task.Result.GetValueOrDefault(ShopAddress);
                if (shop != null)
                {
                    UpdateShop?.Invoke(this, shop);
                }
            }
        }

        public IEnumerator CoTxProcessor()
        {
            var actions = new List<PolymorphicAction<ActionBase>>();

            while (true)
            {
                yield return new WaitForSeconds(TxProcessInterval);
                PolymorphicAction<ActionBase> action;
                while (queuedActions.TryDequeue(out action))
                {
                    actions.Add(action);
                }

                if (actions.Count > 0)
                {
                    var task = Task.Run(() =>
                    {
                        try
                        {
                            StageActions(actions);
                            actions.Clear();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"{e}\n{e.StackTrace}");
                            throw;
                        }
                    });
                    yield return new WaitUntil(() => task.IsCompleted);
                }
            }
        }

        public IEnumerator CoMiner()
        {
            while (true)
            {
                var task = Task.Run(async () =>
                {
                    var tx = Transaction<PolymorphicAction<ActionBase>>.Create(
                        privateKey,
                        new List<PolymorphicAction<ActionBase>>()
                        {
                            new RewardGold() { Gold = rewardAmount }
                        },
                        timestamp: DateTime.UtcNow
                    );
                    blocks.StageTransactions(new HashSet<Transaction<PolymorphicAction<ActionBase>>> {tx});
                    var block = blocks.MineBlock(UserAddress);
                    await swarm.BroadcastBlocksAsync(new[] {block});
                    return block;
                });
                yield return new WaitUntil(() => task.IsCompleted);
                Debug.Log($"created block index: {task.Result.Index}");

#if BLOCK_LOG_USE
                FileHelper.AppendAllText("Block.log", task.Result.ToVerboseString());
#endif
            }
        }

        private void StageActions(IList<PolymorphicAction<ActionBase>> actions)
        {
            var tx = Transaction<PolymorphicAction<ActionBase>>.Create(
                privateKey,
                actions,
                timestamp: DateTime.UtcNow
            );
            blocks.StageTransactions(new HashSet<Transaction<PolymorphicAction<ActionBase>>> {tx});
            swarm.BroadcastTxsAsync(new[] { tx }).Wait();
        }

        public void Dispose()
        {
            swarm?.Dispose();
        }

        private class DebugPolicy : IBlockPolicy<PolymorphicAction<ActionBase>>
        {
            public InvalidBlockException ValidateBlocks(IEnumerable<Block<PolymorphicAction<ActionBase>>> blocks, DateTimeOffset currentTime)
            {
                return null;
            }

            public int GetNextBlockDifficulty(IEnumerable<Block<PolymorphicAction<ActionBase>>> blocks)
            {
                return blocks.Empty() ? 0 : 1;
            }
        }
    }
}
