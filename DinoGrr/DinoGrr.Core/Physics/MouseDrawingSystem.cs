using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using DinoGrr.Core.Rendering;

namespace DinoGrr.Core.Physics;

/// <summary>
/// Handles mouse input for drawing rigid bodies.
/// Captures mouse movements and converts them into rigid body shapes.
/// </summary>
public class MouseDrawingSystem
{
    /// <summary>
    /// Current state of the drawing system.
    /// </summary>
    public enum DrawingState
    {
        Idle,
        Drawing,
        Complete
    }

    /// <summary>
    /// Current drawing state.
    /// </summary>
    public DrawingState State { get; private set; } = DrawingState.Idle;

    /// <summary>
    /// Points captured during the current drawing session.
    /// </summary>
    public List<Vector2> CurrentDrawing { get; private set; } = new List<Vector2>();

    /// <summary>
    /// Minimum distance between captured points to avoid too dense sampling.
    /// </summary>
    public float MinPointDistance { get; set; } = 5f;

    /// <summary>
    /// Minimum number of points required to create a valid rigid body.
    /// </summary>
    public int MinPointCount { get; set; } = 2; // Changed from 3 to 2 to allow lines

    /// <summary>
    /// Maximum number of points to prevent overly complex shapes.
    /// </summary>
    public int MaxPointCount { get; set; } = 50;

    /// <summary>
    /// Simplification tolerance for Douglas-Peucker algorithm.
    /// Higher values create simpler shapes.
    /// </summary>
    public float SimplificationTolerance { get; set; } = 8f;

    /// <summary>
    /// Camera for converting screen coordinates to world coordinates.
    /// </summary>
    private Camera2D _camera;

    /// <summary>
    /// Previous mouse state for detecting state changes.
    /// </summary>
    private MouseState _previousMouseState;

    /// <summary>
    /// Creates a new mouse drawing system.
    /// </summary>
    /// <param name="camera">Camera for coordinate conversion.</param>
    public MouseDrawingSystem(Camera2D camera)
    {
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _previousMouseState = Mouse.GetState();
    }

    /// <summary>
    /// Updates the drawing system with current mouse input.
    /// </summary>
    /// <param name="mouseState">Current mouse state.</param>
    public void Update(MouseState mouseState)
    {
        switch (State)
        {
            case DrawingState.Idle:
                HandleIdleState(mouseState);
                break;
            case DrawingState.Drawing:
                HandleDrawingState(mouseState);
                break;
            case DrawingState.Complete:
                HandleCompleteState(mouseState);
                break;
        }

        _previousMouseState = mouseState;
    }

    /// <summary>
    /// Gets the completed drawing and resets the system.
    /// </summary>
    /// <returns>The completed drawing points, or null if not complete.</returns>
    public List<Vector2> GetCompletedDrawing()
    {
        if (State != DrawingState.Complete)
            return null;

        var result = new List<Vector2>(CurrentDrawing);
        Reset();
        return result;
    }

    /// <summary>
    /// Cancels the current drawing and resets the system.
    /// </summary>
    public void CancelDrawing()
    {
        Reset();
    }

    /// <summary>
    /// Resets the drawing system to idle state.
    /// </summary>
    private void Reset()
    {
        State = DrawingState.Idle;
        CurrentDrawing.Clear();
    }

    /// <summary>
    /// Handles input when the system is idle.
    /// </summary>
    private void HandleIdleState(MouseState mouseState)
    {
        // Start drawing when left mouse button is pressed
        if (mouseState.LeftButton == ButtonState.Pressed &&
            _previousMouseState.LeftButton == ButtonState.Released)
        {
            StartDrawing(mouseState);
        }
    }

    /// <summary>
    /// Handles input when actively drawing.
    /// </summary>
    private void HandleDrawingState(MouseState mouseState)
    {
        // Continue drawing while left button is held
        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            ContinueDrawing(mouseState);
        }
        // Finish drawing when left button is released
        else if (_previousMouseState.LeftButton == ButtonState.Pressed)
        {
            FinishDrawing();
        }

        // Cancel drawing with right click
        if (mouseState.RightButton == ButtonState.Pressed)
        {
            Reset();
        }
    }

    /// <summary>
    /// Handles input when drawing is complete.
    /// </summary>
    private void HandleCompleteState(MouseState mouseState)
    {
        // The drawing is complete and waiting to be retrieved
        // This state is handled by GetCompletedDrawing()
    }

    /// <summary>
    /// Starts a new drawing session.
    /// </summary>
    private void StartDrawing(MouseState mouseState)
    {
        State = DrawingState.Drawing;
        CurrentDrawing.Clear();

        // Convert mouse position to world coordinates
        Vector2 worldPos = _camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
        CurrentDrawing.Add(worldPos);
    }

    /// <summary>
    /// Continues the current drawing session.
    /// </summary>
    private void ContinueDrawing(MouseState mouseState)
    {
        Vector2 worldPos = _camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));

        // Only add point if it's far enough from the last point
        if (CurrentDrawing.Count == 0 ||
            Vector2.Distance(CurrentDrawing.Last(), worldPos) >= MinPointDistance)
        {
            CurrentDrawing.Add(worldPos);

            // Prevent overly complex shapes
            if (CurrentDrawing.Count >= MaxPointCount)
            {
                FinishDrawing();
            }
        }
    }

    /// <summary>
    /// Finishes the current drawing session and processes the result.
    /// </summary>
    private void FinishDrawing()
    {
        if (CurrentDrawing.Count < MinPointCount)
        {
            // Not enough points for a valid shape
            Reset();
            return;
        }

        // Simplify the drawing to reduce complexity
        CurrentDrawing = SimplifyPolyline(CurrentDrawing, SimplificationTolerance);

        // Don't close the shape - keep it exactly as drawn
        if (CurrentDrawing.Count >= MinPointCount)
        {
            State = DrawingState.Complete;
        }
        else
        {
            // Still not enough points after simplification
            Reset();
        }
    }

    /// <summary>
    /// Simplifies a polyline using the Douglas-Peucker algorithm.
    /// </summary>
    /// <param name="points">Original points.</param>
    /// <param name="tolerance">Simplification tolerance.</param>
    /// <returns>Simplified points.</returns>
    private List<Vector2> SimplifyPolyline(List<Vector2> points, float tolerance)
    {
        if (points.Count <= 2)
            return new List<Vector2>(points);

        return DouglasPeucker(points, tolerance);
    }

    /// <summary>
    /// Douglas-Peucker line simplification algorithm.
    /// </summary>
    private List<Vector2> DouglasPeucker(List<Vector2> points, float tolerance)
    {
        if (points.Count <= 2)
            return new List<Vector2>(points);

        // Find the point with the maximum distance from the line segment
        float maxDistance = 0f;
        int maxIndex = 0;

        Vector2 start = points[0];
        Vector2 end = points[points.Count - 1];

        for (int i = 1; i < points.Count - 1; i++)
        {
            float distance = PerpendicularDistance(points[i], start, end);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                maxIndex = i;
            }
        }

        List<Vector2> result = new List<Vector2>();

        // If the maximum distance is greater than tolerance, recursively simplify
        if (maxDistance > tolerance)
        {
            // Recursively simplify both sides
            var leftSide = DouglasPeucker(points.GetRange(0, maxIndex + 1), tolerance);
            var rightSide = DouglasPeucker(points.GetRange(maxIndex, points.Count - maxIndex), tolerance);

            // Combine results (remove duplicate point at the junction)
            result.AddRange(leftSide.GetRange(0, leftSide.Count - 1));
            result.AddRange(rightSide);
        }
        else
        {
            // If the maximum distance is within tolerance, return just the endpoints
            result.Add(start);
            result.Add(end);
        }

        return result;
    }

    /// <summary>
    /// Calculates the perpendicular distance from a point to a line segment.
    /// </summary>
    private float PerpendicularDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        float lineLength = line.Length();

        if (lineLength < 1e-6f)
            return Vector2.Distance(point, lineStart);

        Vector2 lineNorm = line / lineLength;
        Vector2 pointVec = point - lineStart;

        // Project point onto line
        float projection = Vector2.Dot(pointVec, lineNorm);
        projection = MathHelper.Clamp(projection, 0f, lineLength);

        Vector2 closestPoint = lineStart + lineNorm * projection;
        return Vector2.Distance(point, closestPoint);
    }
}
