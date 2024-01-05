using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interpolator : MonoBehaviour
{
    [SerializeField] private float elapsedTime = 0f;
    [SerializeField] private float lerpTime = 0.05f;
    [SerializeField] private float threshold = 0.05f;

    private readonly List<TransformUpdate> transformUpdates = new List<TransformUpdate>();
    private float squareMovemenentThreshold;
    private TransformUpdate to;
    private TransformUpdate from;
    private TransformUpdate previous;


    private void Start()
    {
        squareMovemenentThreshold = threshold * threshold;
        to = new TransformUpdate(Client.instance.ServerTick, transform.position);
        from = new TransformUpdate(Client.instance.InterpolationTick, transform.position);
        previous = new TransformUpdate(Client.instance.InterpolationTick, transform.position);
    }

    private void Update()
    {
        for(int i = 0; i < transformUpdates.Count; i++)
        {
            if (transformUpdates[i].Tick <= Client.instance.InterpolationTick)
            {
                to = transformUpdates[i];
                previous = from = to;
                transform.position = to.Position;
                transformUpdates.RemoveAt(i);
                i--;
                elapsedTime = 0f;
                lerpTime = (to.Tick - from.Tick) * Time.fixedDeltaTime;
            }

        }
        elapsedTime += Time.deltaTime;
        InterpolatePosition(elapsedTime / lerpTime);
    }


    private void InterpolatePosition(float lerpAmount)
    {
        if ((to.Position - previous.Position).sqrMagnitude < squareMovemenentThreshold)
        {
            if(to.Position != from.Position)
                transform.position = Vector3.Lerp(from.Position, to.Position, lerpAmount);


            return;
        }
        transform.position = Vector3.LerpUnclamped(from.Position, to.Position, lerpAmount);
    }

    public void newUpdate(int tick, Vector3 position)
    {
        if(tick <= Client.instance.InterpolationTick)
        {
            return;
        }

        for(int i = 0; i < transformUpdates.Count; i++)
        {
            if (transformUpdates[i].Tick > tick)
            {
                transformUpdates.Insert(i, new TransformUpdate(tick, position));
                return;
            }
        }

        transformUpdates.Add(new TransformUpdate(tick, position));
    }
}
