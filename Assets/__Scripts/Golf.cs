using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;   // We’ll need this line later in the chapter

[RequireComponent(typeof(Deck))]                                              // a
[RequireComponent(typeof(JsonParseLayout))]
public class Golf : MonoBehaviour
{
    private static Golf S; // A private Singleton for Prospector

    [Header("Dynamic")]
    public List<CardGolf> drawPile;

    public List<CardGolf> discardPile;
    public List<CardGolf> mine;
    public CardGolf target;

    private Transform layoutAnchor;

    private Deck deck;
    private JsonLayout jsonLayout;

    // A Dictionary to pair mine layout IDs and actual Cards
    private Dictionary<int, CardGolf> mineIdToCardDict;                 // a


    void Start()
    {
        // Set the private Singleton. We’ll use this later.
        if (S != null) Debug.LogError("Attempted to set S more than once!");  // b
        S = this;

        jsonLayout = GetComponent<JsonParseLayout>().layout;

        deck = GetComponent<Deck>();
        // These two lines replace the Start() call we commented out in Deck
        deck.InitDeck();
        Deck.Shuffle(ref deck.cards);

        drawPile = ConvertCardsToCardGolfs(deck.cards);

        LayoutMine();

        MoveToTarget(Draw());
        UpdateDrawPile();
    }

    /// <summary>
    /// Converts each Card in a List(Card) into a List(CardProspector) so that it
    ///  can be used in the Prospector game.
    /// </summary>
    /// <param name="listCard">A List(Card) to be converted</param>
    /// <returns>A List(CardProspector) of the converted cards</returns>
    List<CardGolf> ConvertCardsToCardGolfs(List<Card> listCard)
    {
        List<CardGolf> listCG = new List<CardGolf>();
        CardGolf cg;
        foreach (Card card in listCard)
        {
            cg = card as CardGolf;                                      // c
            listCG.Add(cg);
        }
        return (listCG);
    }

    /// <summary>
    /// Pulls a single card from the beginning of the drawPile and returns it
    /// Note: There is no protection against trying to draw from an empty pile!
    /// </summary>
    /// <returns>The top card of drawPile</returns>
    CardGolf Draw()
    {
        CardGolf cg = drawPile[0]; // Pull the 0th CardProspector
        drawPile.RemoveAt(0);            // Then remove it from drawPile
        return (cg);                      // And return it
    }

    /// <summary>
    /// Positions the initial tableau of cards, a.k.a. the "mine"
    /// </summary>
    void LayoutMine()
    {
        // Create an empty GameObject to serve as an anchor for the tableau   // a
        if (layoutAnchor == null)
        {
            // Create an empty GameObject named _LayoutAnchor in the Hierarchy
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;             // Grab its Transform
        }

        CardGolf cg;

        // Generate the Dictionary to match mine layout ID to CardProspector
        mineIdToCardDict = new Dictionary<int, CardGolf>();             // b


        // Iterate through the JsonLayoutSlots pulled from the JSON_Layout
        foreach (JsonLayoutSlot slot in jsonLayout.slots)
        {
            cg = Draw(); // Pull a card from the top (beginning) of the draw Pile
            cg.faceUp = slot.faceUp;    // Set its faceUp to the value in SlotDef
                                        // Make the CardProspector a child of layoutAnchor
            cg.transform.SetParent(layoutAnchor);

            // Convert the last char of the layer string to an int (e.g. "Row 0")
            int z = int.Parse(slot.layer[slot.layer.Length - 1].ToString());  // c

            // Set the localPosition of the card based on the slot information
            cg.SetLocalPos(new Vector3(
            jsonLayout.multiplier.x * slot.x,
            jsonLayout.multiplier.y * slot.y,
            -z));                                                       // d

            cg.layoutID = slot.id;
            cg.layoutSlot = slot;
            // CardProspectors in the mine have the state CardState.mine
            cg.state = eCardStateG.mine;

            // Set the sorting layer of all SpriteRenderers on the Card
            cg.SetSpriteSortingLayer(slot.layer);

            mine.Add(cg); // Add this CardProspector to the List<mine>

            // Add this CardProspector to the mineIDtoCardDict Dictionary
            mineIdToCardDict.Add(slot.id, cg);                                // c

        }
    }

    /// <summary>
    /// Moves the current target card to the discardPile
    /// </summary>
    /// <param name="cp">The CardProspector to be moved</param>
    void MoveToDiscard(CardGolf cg)
    {
        // Set the state of the card to discard
        cg.state = eCardStateG.discard;
        discardPile.Add(cg);  // Add it to the discardPile List<>
        cg.transform.SetParent(layoutAnchor); // Update its transform parent

        // Position it on the discardPile
        cg.SetLocalPos(new Vector3(
        jsonLayout.multiplier.x * jsonLayout.discardPile.x,
        jsonLayout.multiplier.y * jsonLayout.discardPile.y,
        0));

        cg.faceUp = true;

        // Place it on top of the pile for depth sorting
        cg.SetSpriteSortingLayer(jsonLayout.discardPile.layer);               // a
        cg.SetSortingOrder(-200 + (discardPile.Count * 3));                  // b
    }

    /// <summary>
    /// Make cp the new target card
    /// </summary>
    /// <param name="cp">The CardProspector to be moved</param>
    void MoveToTarget(CardGolf cg)
    {
        // If there is currently a target card, move it to discardPile
        if (target != null) MoveToDiscard(target);

        // Use MoveToDiscard to move the target card to the correct location
        MoveToDiscard(cg);                                                    // c

        // Then set a few additional things to make cp the new target
        target = cg; // cp is the new target
        cg.state = eCardStateG.target;

        // Set the depth sorting so that cp is on top of the discardPile
        cg.SetSpriteSortingLayer("Target");                                 // c
        cg.SetSortingOrder(0);
    }

    /// <summary>
    /// Arranges all the cards of the drawPile to show how many are left
    /// </summary>
    void UpdateDrawPile()
    {
        CardGolf cg;
        // Go through all the cards of the drawPile
        for (int i = 0; i < drawPile.Count; i++)
        {
            cg = drawPile[i];
            cg.transform.SetParent(layoutAnchor);

            // Position it correctly with the layout.drawPile.stagger
            Vector3 cgPos = new Vector3();
            cgPos.x = jsonLayout.multiplier.x * jsonLayout.drawPile.x;
            // Add the staggering for the drawPile
            cgPos.x += jsonLayout.drawPile.xStagger * i;
            cgPos.y = jsonLayout.multiplier.y * jsonLayout.drawPile.y;
            cgPos.z = 0.1f * i;
            cg.SetLocalPos(cgPos);

            cg.faceUp = false; // DrawPile Cards are all face-down
            cg.state = eCardStateG.drawpile;
            // Set depth sorting
            cg.SetSpriteSortingLayer(jsonLayout.drawPile.layer);
            cg.SetSortingOrder(-10 * i);
        }
    }

    /// <summary>
    /// This turns cards in the Mine face-up and face-down
    /// </summary>
    public void SetMineFaceUps()
    {                                            // d
        CardGolf coverCG;
        foreach (CardGolf cg in mine)
        {
            bool faceUp = true; // Assume the card will be face-up

            // Iterate through the covering cards by mine layout ID
            foreach (int coverID in cg.layoutSlot.hiddenBy)
            {
                coverCG = mineIdToCardDict[coverID];
                // If the covering card is null or still in the mine...
                if (coverCG == null || coverCG.state == eCardStateG.mine)
                {
                    faceUp = false; // then this card is face-down
                }
            }
            cg.faceUp = faceUp; // Set the value on the card
        }
    }



    /// <summary>
    /// Handler for any time a card in the game is clicked
    /// </summary>
    /// <param name="cp">The CardProspector that was clicked</param>
    static public void CARD_CLICKED(CardGolf cg)
    {
        // The reaction is determined by the state of the clicked card
        switch (cg.state)
        {
            case eCardStateG.target:
                // Clicking the target card does nothing
                break;
            case eCardStateG.drawpile:
                // Clicking *any* card in the drawPile will draw the next card
                // Call two methods on the Prospector Singleton S
                S.MoveToTarget(S.Draw());  // Draw a new target card
                S.UpdateDrawPile();          // Restack the drawPile
                break;
            case eCardStateG.mine:
                // Clicking a card in the mine will check if it’s a valid play
                bool validMatch = true;  // Initially assume that it’s valid 

                // If the card is face-down, it’s not valid
                if (!cg.faceUp) validMatch = false;

                // If it’s not an adjacent rank, it’s not valid
                if (!cg.AdjacentTo(S.target)) validMatch = false;            // b

                if (validMatch)
                {        // If it’s a valid card
                    S.mine.Remove(cg);   // Remove it from the tableau List
                    S.MoveToTarget(cg);  // Make it the target card

                    S.SetMineFaceUps();  // Be sure to add this line!!
                }
                break;
        }
    }

}