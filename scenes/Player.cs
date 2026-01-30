using Godot;
using System;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using static Godot.TextServer;

public partial class Player : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 7.5f;

	private Vector3 latestValidInput = Vector3.Forward;

	[Export]
	Camera3D Camera;

	[Export]
	Node3D meshInstance;

	[Export]
	Timer coyoteTimer;

    [Export]
    Timer airTimer;

	[Export]
	Timer preJumpTimer;

	[Export]
	Node3D playerInstance;

    [Export]
    ShapeCast3D verticalRay;


    private bool crouchAnimation = false;
    private bool unCrouchAnimation = false;


    private Vector3 slerpedInput = Vector3.Forward;

    private bool canCoyote = false;

	private bool justJumped = false;

	private Vector3 crouchScale = new Vector3(0, 0.5f, 0);
	private bool isCrouched = false;


    private Vector3 originalScale = new Vector3(1, 1, 1);


    private float maxSpeed = 10.0f;
    private float defaultMaxSpeed = 8.0f;
    private float crouchSpeed = 2.0f;
    private float acceleration = 5.0f;


    enum Movement_Type
    {
        INSTANT,
        CONSTANT,
        EASE
    }
    private Movement_Type movementType = Movement_Type.INSTANT;

    public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

        //meshInstance.LookAt(latestValidInput);






        // Movement type selector
        if (Input.IsActionJustPressed("INSTANT"))
        {
            movementType = Movement_Type.INSTANT;
        }
        if (Input.IsActionJustPressed("CONSTANT"))
        {
            movementType = Movement_Type.CONSTANT;
        }
        if (Input.IsActionJustPressed("EASE"))
        {
            movementType = Movement_Type.EASE;
        }









        // Add the gravity.
        if (!IsOnFloor())
		{
			if (airTimer.IsStopped() || velocity.Y > 0)
            {
                velocity += GetGravity() * (float)delta;
            }

			if (canCoyote)
            {
                coyoteTimer.Start();
				canCoyote = false;
            }
		}
		else
		{
            canCoyote = true;
        }





		// Handle Jump.
		if (Input.IsActionJustPressed("jump") && !justJumped && (IsOnFloor() || !coyoteTimer.IsStopped()) && velocity.Y <= 0)
		{
			airTimer.Start();
            preJumpTimer.Start();
            coyoteTimer.Stop();
			if(!isCrouched)
            {
                isCrouched = true;
                crouchAnimation = true;
            }

            canCoyote = false;

            justJumped = true;

        }

		if (preJumpTimer.IsStopped() && justJumped)
        {
            velocity.Y = JumpVelocity;
            if (isCrouched)
            {
                isCrouched = false;
                unCrouchAnimation = true;
            }
            justJumped = false;
        }


        // Crouching logic
		if(Input.IsActionPressed("crouch") && IsOnFloor())
        {
			if(!isCrouched)
            {
                isCrouched = true;
                crouchAnimation = true;
            }
            maxSpeed = crouchSpeed;
        }
		else
        {
			if(isCrouched && preJumpTimer.IsStopped())
            {
                isCrouched = false;
                unCrouchAnimation = true;
            }
            maxSpeed = defaultMaxSpeed;
        }



        // Crouch animation
        if (playerInstance.Scale.Y > 0.5f && crouchAnimation && !unCrouchAnimation)
        {
            playerInstance.Scale -= crouchScale * 0.3f;
            if (playerInstance.Scale.Y < 0.5f) playerInstance.Scale = originalScale - crouchScale;
        }
        else
        {
            crouchAnimation = false;
        }


        // Uncrouch animation
        if (playerInstance.Scale.Y < 1.0f && unCrouchAnimation && !crouchAnimation && !verticalRay.IsColliding())
        {
            playerInstance.Scale += crouchScale * 0.3f;
            if (playerInstance.Scale.Y > 1.0f) playerInstance.Scale = originalScale;
        }
        else
        {
            unCrouchAnimation = false;

        }









        // Movement

        Vector2 inputDir = Input.GetVector("turn_left", "turn_right", "move_forward", "move_backward");

		Vector3 direction = new Vector3();

		direction += inputDir.X * Camera.Transform.Basis.X;
		Vector3 forward = Camera.Transform.Basis.Z;
		forward.Y = 0.0f;
        direction += inputDir.Y * forward.Normalized();

        //Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized() * Camera.Transform.Basis.Z;
        if (direction != Vector3.Zero)
		{
            //velocity.X = direction.X * Speed;
            //velocity.Z = direction.Z * Speed;
            switch(movementType)
            {
                case Movement_Type.INSTANT:
                    velocity.X = direction.X * maxSpeed;
                    velocity.Z = direction.Z * maxSpeed;
                    break;

                case Movement_Type.CONSTANT:
                    velocity = velocity.MoveToward(direction * maxSpeed, acceleration * 2 * (float)delta);
                    break;

                case Movement_Type.EASE:
                    velocity = velocity.Lerp(direction * maxSpeed, acceleration * 0.5f * (float)delta);
                    break;

            }

		}
		else
		{
            //velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            //velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
            //velocity = velocity.Lerp(direction * 0, acceleration * (float)delta);

            switch (movementType)
            {
                case Movement_Type.INSTANT:
                    velocity.X = direction.X * 0;
                    velocity.Z = direction.Z * 0;
                    break;

                case Movement_Type.CONSTANT:
                    velocity = velocity.MoveToward(direction * 0, acceleration * 2 * (float)delta);
                    break;

                case Movement_Type.EASE:
                    velocity = velocity.Lerp(direction * 0, acceleration * 0.5f * (float)delta);
                    break;

            }

        }


        GD.Print(velocity);


        if (direction.Length() > 0.1f)
        {
            latestValidInput = direction;
            
        }

        DebugDraw3D.DrawArrow(Vector3.Zero, latestValidInput * 3, Colors.Yellow, 0.5f, true);

        Basis meshBasis = meshInstance.Basis;
		Vector3 meshForward = meshInstance.GlobalBasis.Z;
        slerpedInput = slerpedInput.Slerp(latestValidInput, (float)delta * 10);

        DebugDraw3D.DrawArrow(Vector3.Zero, meshForward * 3, Colors.Blue, 0.5f, true);
        DebugDraw3D.DrawArrow(Vector3.Zero, slerpedInput * 3, Colors.Red, 0.5f, true);

      

        meshInstance.LookAt(slerpedInput + meshInstance.GlobalPosition, Vector3.Up); // meshInstance.GlobalPosition
		

        DebugDraw3D.DrawSphere(direction + meshInstance.GlobalPosition, 0.5f, Colors.Red);
			



        Velocity = velocity;
		MoveAndSlide();
	}



	private void RotateMesh(Vector3 inputDirection, double delta)
	{

		DebugDraw3D.DrawArrow(Vector3.Zero, inputDirection, Colors.Red,1.0f,true);
		

    }




    




}
