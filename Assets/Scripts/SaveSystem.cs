using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 플레이어 데이터를 JSON 파일로 저장·로드한다.
/// 저장 위치는 Application.persistentDataPath (플랫폼별 사용자 폴더, 빌드 후에도 쓰기 가능).
///
/// 저장 파일이 없으면 "모든 카드 소지 + 기본 덱"으로 초기화한다(지금은 전부 소지 상태).
/// </summary>
public static class SaveSystem
{
    private const string FileName = "player_save.json";

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>저장 파일을 읽는다. 없거나 깨져 있으면 database 기준으로 새로 만든다.</summary>
    public static PlayerSaveData Load(CardDatabase database)
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                var data = JsonUtility.FromJson<PlayerSaveData>(json);
                if (data != null)
                {
                    Debug.Log($"[Save] 불러옴: 소지 {data.ownedCardIds.Count}종, 덱 {data.decks.Count}개");
                    return data;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Save] 불러오기 실패({e.Message}). 새로 만듭니다.");
        }

        var fresh = CreateDefault(database);
        Save(fresh);
        return fresh;
    }

    /// <summary>현재 데이터를 JSON 파일로 저장한다.</summary>
    public static void Save(PlayerSaveData data)
    {
        if (data == null) return;
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(FilePath, json);
            Debug.Log($"[Save] 저장 완료 → {FilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Save] 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 새 저장 데이터를 만든다. 지금은 모든 카드를 소지 상태로 둔다.
    /// (나중에 카드 해금 시스템이 생기면 초기 소지 목록만 좁히면 된다.)
    /// </summary>
    public static PlayerSaveData CreateDefault(CardDatabase database)
    {
        var data = new PlayerSaveData();

        if (database != null)
        {
            foreach (var card in database.AllCards)
            {
                if (card == null || string.IsNullOrEmpty(card.id)) continue;
                if (card.Type == CardType.Field) continue;   // 필드 카드는 플레이어 소지 대상이 아님
                data.AddOwned(card.id);
            }
        }

        data.decks.Add(BuildStarterDeck(database, data));
        data.selectedDeckIndex = 0;

        Debug.Log($"[Save] 새 데이터 생성: 소지 {data.ownedCardIds.Count}종");
        return data;
    }

    /// <summary>규칙(5~10장, 같은 카드 3장, 패시브 1장)을 만족하는 시작 덱을 자동 구성한다.</summary>
    private static DeckSaveData BuildStarterDeck(CardDatabase database, PlayerSaveData data)
    {
        var deck = new DeckSaveData("기본 덱", null);
        if (database == null) return deck;

        var picked = new List<CardData>();

        // 소지한 카드를 규칙에 맞게 채운다(최소 장수를 넘길 때까지).
        foreach (var card in database.AllCards)
        {
            if (card == null || !data.Owns(card.id)) continue;

            // 같은 카드를 최대 2장까지 넣어 최소 장수를 채운다(3장 한도 안에서 여유를 둠).
            for (int copy = 0; copy < 2; copy++)
            {
                if (picked.Count >= DeckRules.MinCards) break;
                if (!DeckRules.CanAdd(picked, card).Valid) break;
                picked.Add(card);
                deck.cardIds.Add(card.id);
            }
            if (picked.Count >= DeckRules.MinCards) break;
        }

        var check = DeckRules.Validate(picked);
        if (!check.Valid)
            Debug.LogWarning($"[Save] 기본 덱이 규칙을 만족하지 못했습니다: {check.Reason}");

        return deck;
    }

    /// <summary>저장 파일을 지운다(초기화 테스트용).</summary>
    public static void DeleteSave()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
            Debug.Log("[Save] 저장 파일 삭제됨.");
        }
    }
}