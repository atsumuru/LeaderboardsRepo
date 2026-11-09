using System.Collections;
using UnityEngine;
using PlayFab;
using PlayFab.MultiplayerModels;
using TMPro;

public class Matchmaker : MonoBehaviour
{
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject leaveQueueButton;
    [SerializeField] private TMP_Text queueStatusText;

    private string ticketId;
    private Coroutine pollTicketCoroutine;

    private const string QueueName = "DefaultQueue";

    public void StartMatchmaking()
    {
        playButton.SetActive(false);
        queueStatusText.text = "Submitting Ticket";
        queueStatusText.gameObject.SetActive(true);

        PlayFabMultiplayerAPI.CreateMatchmakingTicket(
            new CreateMatchmakingTicketRequest
            {
                Creator = new MatchmakingPlayer
                {
                    Entity = new EntityKey
                    {
                        Id = PlayFabLogin.EntityId,
                        Type = "title_player_account"
                    },
                    Attributes = new MatchmakingPlayerAttributes
                    {
                        DataObject = new { }
                    }
                },

                GiveUpAfterSeconds = 120,
                QueueName = QueueName
            },
            OnMatchingTicketCreated,
            OnMatchmakingError,
            null,
            null
        );
    }

    private void OnMatchingTicketCreated(CreateMatchmakingTicketResult result)
    {
        ticketId = result.TicketId;
        pollTicketCoroutine = StartCoroutine(PollTicket());

        leaveQueueButton.SetActive(true);
        queueStatusText.text = "Ticket Created";
    }

    private void OnMatchmakingError (PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }

    private IEnumerator PollTicket()
    {
        while(true)
        {
            PlayFabMultiplayerAPI.GetMatchmakingTicket(
                new GetMatchmakingTicketRequest
                {
                    TicketId = ticketId,
                    QueueName = QueueName
                },
                OnGetMatchmakingTicket,
                OnMatchmakingError
            );

            yield return new WaitForSeconds(6);
        }
    }

    private void OnGetMatchmakingTicket(GetMatchmakingTicketResult result)
    {
        queueStatusText.text = $"Status: {result.Status}";

        switch (result.Status)
        {
            case "Matched":
                StopCoroutine(pollTicketCoroutine);
                StartMatch(result.MatchId);
                break;
            case "Canceled":
                break;
        }
    }

    private void StartMatch(string matchId)
    {
        queueStatusText.text = $"Starting Match";

        PlayFabMultiplayerAPI.GetMatch(
            new GetMatchRequest
            {
                MatchId = matchId,
                QueueName = QueueName
            },
            OnGetMatch,
            OnMatchmakingError,
            null,
            null
        );
    }

    private void OnGetMatch(GetMatchResult result)
    {
        queueStatusText.text = $"{result.Members[0].Entity.Id} vs {result.Members[1].Entity.Id}";
    }
}
