
namespace ConquestGame
{
    public class ConquestGameModeTeamsSpawnRequest {
        public long PlayerId  { get; set; }
        public long FactionId  { get; set; }

        public ushort CountdownTimer  { get; set; }
        public string SpawnPrefab;

        public ConquestGameModeTeamsSpawnRequest(long playerId, long factionId)
        {
            CountdownTimer = OPTIONS.SpawnTimerCountdown;
            PlayerId = playerId;
            FactionId = factionId;
            SpawnPrefab = OPTIONS.SpawnVehiclePrefab;
        }

        public void Tick() {
            CountdownTimer--;
        }

        public bool IsReady() {
            if (CountdownTimer < 1) {
                return true;
            }
            return false;
        }
    }

}