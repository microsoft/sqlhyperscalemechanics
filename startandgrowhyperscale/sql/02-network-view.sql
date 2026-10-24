-- Connect directly to caldova-scheduling. All three databases must be pooled.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER VIEW network.AppointmentAvailability AS
SELECT p.ProviderKey, p.ProviderName, s.SlotId, p.Location,
       s.Clinician, s.Specialty, s.SlotStart, s.DurationMinutes
FROM network.ProviderClinic AS p
JOIN dbo.AppointmentSlot AS s ON s.ClinicId = p.ClinicId
WHERE p.ProviderKey = N'caldova' AND p.IsNetworkMember = 1 AND s.IsBooked = 0
UNION ALL
SELECT p.ProviderKey, p.ProviderName, s.SlotId, p.Location,
       s.Clinician, s.Specialty, s.SlotStart, s.DurationMinutes
FROM network.ProviderClinic AS p
JOIN [northbrook-scheduling].dbo.AppointmentSlot AS s ON s.ClinicId = p.ClinicId
WHERE p.ProviderKey = N'northbrook' AND p.IsNetworkMember = 1 AND s.IsBooked = 0
UNION ALL
SELECT p.ProviderKey, p.ProviderName, s.SlotId, p.Location,
       s.Clinician, s.Specialty, s.SlotStart, s.DurationMinutes
FROM network.ProviderClinic AS p
JOIN [ridgeview-scheduling].dbo.AppointmentSlot AS s ON s.ClinicId = p.ClinicId
WHERE p.ProviderKey = N'ridgeview' AND p.IsNetworkMember = 1 AND s.IsBooked = 0;
GO
