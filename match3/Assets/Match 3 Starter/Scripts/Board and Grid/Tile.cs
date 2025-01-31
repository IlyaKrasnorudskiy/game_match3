﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tile : MonoBehaviour {
	private static Color selectedColor = new Color(.5f, .5f, .5f, 1.0f);
	private static Tile previousSelected = null;

	private SpriteRenderer render;
	private bool isSelected = false;

	private Vector2[] adjacentDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

	void Awake() {
		render = GetComponent<SpriteRenderer>();
    }

	private void Select() {
		isSelected = true;
		render.color = selectedColor;
		previousSelected = gameObject.GetComponent<Tile>();
		SFXManager.instance.PlaySFX(Clip.Select);
	}

	private IEnumerator ProcessMove(Tile otherTile) {
    SwapSprite(otherTile.render);
    yield return new WaitForSeconds(0.2f); // Задержка перед проверкой

    bool matchFound = otherTile.ClearAllMatches() || ClearAllMatches();
    if (!matchFound) {
        SwapSprite(otherTile.render); // Возвращаем элементы на прежние места
    } else {
        GUIManager.instance.MoveCounter--; // Уменьшаем счетчик ходов только при успешном ходе
    }

    BoardManager.instance.IsShifting = false;
}

	private void Deselect() {
		isSelected = false;
		render.color = Color.white;
		previousSelected = null;
	}

	public void OnMouseDown() {
    if (render.sprite == null || BoardManager.instance.IsShifting) {
        return;
    }

    if (isSelected) {
        Deselect();
    } else {
        if (previousSelected == null) {
            Select();
        } else {
            if (GetAllAdjacentTiles().Contains(previousSelected.gameObject)) {
                BoardManager.instance.IsShifting = true;
                StartCoroutine(ProcessMove(previousSelected));
                previousSelected.Deselect();
            } else {
                previousSelected.GetComponent<Tile>().Deselect();
                Select();
            }
        }
    }
}

public void SwapSprite(SpriteRenderer render2) {
    if (render.sprite == render2.sprite) {
        return;
    }

    Sprite tempSprite = render2.sprite;
    render2.sprite = render.sprite;
    render.sprite = tempSprite;
    SFXManager.instance.PlaySFX(Clip.Swap);
}

	private GameObject GetAdjacent(Vector2 castDir) {
		RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir);
		if (hit.collider != null) {
			return hit.collider.gameObject;
		}
		return null;
	}

	private List<GameObject> GetAllAdjacentTiles() {
		List<GameObject> adjacentTiles = new List<GameObject>();
		for (int i = 0; i < adjacentDirections.Length; i++) {
			adjacentTiles.Add(GetAdjacent(adjacentDirections[i]));
		}
		return adjacentTiles;
	}

	private List<GameObject> FindMatch(Vector2 castDir) {
		List<GameObject> matchingTiles = new List<GameObject>();
		RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir);
		while (hit.collider != null && hit.collider.GetComponent<SpriteRenderer>().sprite == render.sprite) {
			matchingTiles.Add(hit.collider.gameObject);
			hit = Physics2D.Raycast(hit.collider.transform.position, castDir);
		}
		return matchingTiles;
	}

	private bool ClearMatch(Vector2[] paths) {
    List<GameObject> matchingTiles = new List<GameObject>();
    for (int i = 0; i < paths.Length; i++) {
        matchingTiles.AddRange(FindMatch(paths[i]));
    }
    if (matchingTiles.Count >= 2) {
        for (int i = 0; i < matchingTiles.Count; i++) {
            matchingTiles[i].GetComponent<SpriteRenderer>().sprite = null;
        }
        return true;
    }
    return false;
}

	private bool matchFound = false;
	public bool ClearAllMatches() {
    if (render.sprite == null) {
        return false;
    }

    bool matchFound = ClearMatch(new Vector2[2] { Vector2.left, Vector2.right }) || 
                      ClearMatch(new Vector2[2] { Vector2.up, Vector2.down });
    if (matchFound) {
        render.sprite = null;
        StopCoroutine(BoardManager.instance.FindNullTiles());
        StartCoroutine(BoardManager.instance.FindNullTiles());
        SFXManager.instance.PlaySFX(Clip.Clear);
    }
    return matchFound;
}

}