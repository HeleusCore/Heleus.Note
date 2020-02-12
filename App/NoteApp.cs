using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Cryptography;
using Heleus.Network.Client;
using Heleus.Network.Client.Record;
using Heleus.NoteService;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Apps.Shared
{
    public class NoteApp : AppBase<NoteApp>
    {
        readonly Dictionary<string, Dictionary<long, NoteData>> _notes = new Dictionary<string, Dictionary<long, NoteData>>();

        protected override async Task ServiceNodesLoaded(ServiceNodesLoadedEvent arg)
        {
            await base.ServiceNodesLoaded(arg);
            await UIApp.Current.SetFinishedLoading();

            UIApp.Run(() => DownloadNoteData(false));
        }

        public override void UpdateSubmitAccounts()
        {
            var index = NoteServiceInfo.NoteIndex;

            foreach (var serviceNode in ServiceNodeManager.Current.ServiceNodes)
            {
                foreach (var serviceAccount in serviceNode.ServiceAccounts.Values)
                {
                    var keyIndex = serviceAccount.KeyIndex;

                    if (!serviceNode.HasSubmitAccount(keyIndex, index))
                    {
                        serviceNode.AddSubmitAccount(new SubmitAccount(serviceNode, keyIndex, index, true));
                    }
                }
            }

            UIApp.Run(GenerateDefaultSecretKeys);
        }

        async Task GenerateDefaultSecretKeys()
        {
            var index = NoteServiceInfo.NoteIndex;
            var submitAccounts = ServiceNodeManager.Current.GetSubmitAccounts<SubmitAccount>();

            foreach(var submitAccount in submitAccounts)
            {
                var serviceAccount = submitAccount.ServiceAccount;
                var secretKeyManager = submitAccount.SecretKeyManager;
                if (!secretKeyManager.HasSecretKeyType(index, SecretKeyInfoTypes.PublicServiceAccount))
                {
                    var secretKey = await PublicServiceAccountKeySecretKeyInfo.NewSignedPublicKeySecretKey((serviceAccount as ServiceAccountKeyStore).SignedPublicKey, serviceAccount.DecryptedKey);
                    secretKeyManager.AddSecretKey(index, secretKey, true);
                }
            }
        }

        public override ServiceNode GetLastUsedServiceNode(string key = "default")
        {
            var node = base.GetLastUsedServiceNode(key);
            if (node != null)
                return node;

            return ServiceNodeManager.Current.FirstServiceNode;
        }

        public override T GetLastUsedSubmitAccount<T>(string key = "default")
        {
            var account = base.GetLastUsedSubmitAccount<T>(key);
            if (account != null)
                return account;

            var node = GetLastUsedServiceNode();
            if (node != null)
                return node.GetSubmitAccounts<T>().FirstOrDefault();

            return null;
        }

        public async Task<HeleusClientResponse> UploadNote(SubmitAccount submitAccount, string text)
        {
            var serviceNode = submitAccount?.ServiceNode;
            var result = await SetSubmitAccount(submitAccount, true);
            if (result != null)
                goto end;

            if (string.IsNullOrWhiteSpace(text))
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.Ok, (long)NoteUserCodes.InvalidAttachement);
                goto end;
            }

            var secretKey = submitAccount.DefaultSecretKey;
            var attachements = serviceNode.Client.NewAttachements();
            var note = await EncrytpedRecord<NoteRecord>.EncryptRecord(secretKey, new NoteRecord(text));

            attachements.AddBinaryAttachement(NoteServiceInfo.NoteFileName, note.ToByteArray());

            result = await serviceNode.Client.UploadAttachements(attachements, (transaction) =>
            {
                transaction.PrivacyType = DataTransactionPrivacyType.PrivateData;
                transaction.EnableFeature<AccountIndex>(AccountIndex.FeatureId).Index = NoteServiceInfo.NoteIndex;
            });

        end:

            await UIApp.PubSub.PublishAsync(new NoteUploadedEvent(result));

            return result;
        }

        public async Task DownloadNoteData(bool queryOlder)
        {
            await DownloadAccountIndexTransactions(NoteServiceInfo.ChainIndex, NoteServiceInfo.NoteIndex, (download) => download.QueryOlder = queryOlder);
        }

        public NoteData GetNoteData(TransactionDownloadData<Transaction> transaction)
        {
            var serviceNode = transaction.Tag as ServiceNode;
            if (!_notes.TryGetValue(serviceNode.Id, out var lookup))
            {
                lookup = new Dictionary<long, NoteData>();
                _notes[serviceNode.Id] = lookup;
            }

            if (lookup.TryGetValue(transaction.TransactionId, out var noteData))
                return noteData;

            noteData = new NoteData(transaction, serviceNode);
            lookup[transaction.TransactionId] = noteData;

            return noteData;
        }
    }
}
