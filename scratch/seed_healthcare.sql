-- AbilityConnect BD: Section 5.5 Healthcare & Therapy Finder Seed Data

DO $$
DECLARE
    v_admin_id integer;
    v_org_id integer;
    v_user_id integer;
    v_p1_id integer;
    v_p2_id integer;
    v_p3_id integer;
    v_p4_id integer;
    v_p5_id integer;
    v_p6_id integer;
    v_p7_id integer;
    v_p8_id integer;
    v_p9_id integer;
    v_appt_completed_id integer;
BEGIN
    SELECT "Id" INTO v_admin_id FROM "Admins" LIMIT 1;
    SELECT "Id" INTO v_org_id FROM "Organizations" LIMIT 1;
    SELECT "Id" INTO v_user_id FROM "DisabilityUsers" LIMIT 1;

    -- Clean up previous seed healthcare test data if exists
    TRUNCATE "HealthcareReviews", "HealthcareAppointments", "HealthcareProviders" RESTART IDENTITY CASCADE;

    -- 1. Doctor (Dhaka)
    INSERT INTO "HealthcareProviders" (
        "Name", "ProviderType", "OrganizationOrClinic", "Description", "Specialization",
        "Phone", "Email", "Address", "City", "District", "Latitude", "Longitude",
        "AvailableServices", "ConsultationFee", "AvailabilitySchedule",
        "ProfileImage", "OrganizationId", "CreatedAt"
    ) VALUES (
        'Dr. Mahbubur Rahman', 'Doctor', 'Evercare Hospital Dhaka',
        'Senior Consultant with over 15 years experience in stroke neurology and physical medicine. Dedicated wheelchair-accessible clinic on ground floor.',
        'Neurology & Stroke Rehabilitation',
        '+880 1711 000001', 'dr.mahbub@evercare.bd', 'Plot 81, Block E, Bashundhara R/A', 'Bashundhara', 'Dhaka',
        23.8103, 90.4125,
        'Stroke Rehab, Neurological Assessment, Muscle Spasticity Management, Medication Review',
        1000.00, 'Sat - Wed: 4:00 PM - 8:00 PM',
        NULL, v_org_id, NOW()
    ) RETURNING "Id" INTO v_p1_id;

    -- 2. Doctor (Chittagong)
    INSERT INTO "HealthcareProviders" (
        "Name", "ProviderType", "OrganizationOrClinic", "Description", "Specialization",
        "Phone", "Email", "Address", "City", "District", "Latitude", "Longitude",
        "AvailableServices", "ConsultationFee", "AvailabilitySchedule",
        "ProfileImage", "OrganizationId", "CreatedAt"
    ) VALUES (
        'Dr. Fahmida Sultana', 'Doctor', 'Apollo Clinic Chittagong',
        'Pediatric Neurologist specializing in cerebral palsy, neurodevelopmental delays, and pediatric physical rehabilitation with family-centered care.',
        'Pediatric Neurology & Developmental Care',
        '+880 1812 000002', 'dr.fahmida@apolloclinic.bd', 'O.R. Nizam Road, GEC Circle', 'Nasirabad', 'Chittagong',
        22.3569, 91.7832,
        'Cerebral Palsy Management, Child Development Evaluation, Epilepsy Care, Spasticity Therapy',
        800.00, 'Sun - Thu: 3:00 PM - 7:00 PM',
        NULL, v_org_id, NOW()
    ) RETURNING "Id" INTO v_p2_id;

    -- 3. Physiotherapist (Dhaka)
    INSERT INTO "HealthcareProviders" (
        "Name", "ProviderType", "OrganizationOrClinic", "Description", "Specialization",
        "Phone", "Email", "Address", "City", "District", "Latitude", "Longitude",
        "AvailableServices", "ConsultationFee", "AvailabilitySchedule",
        "ProfileImage", "OrganizationId", "CreatedAt"
    ) VALUES (
        'Md. Zahidul Islam, PT', 'Physiotherapist', 'Centre for Physical Therapy & Rehab',
        'Certified physical therapist focused on post-injury mobility recovery, spine care, and assistive mobility equipment fitting for wheelchair users.',
        'Orthopedic & Neurological Physical Therapy',
        '+880 1913 000003', 'zahid.pt@cptr-bd.org', 'House 45, Road 27, Dhanmondi', 'Dhanmondi', 'Dhaka',
        23.7508, 90.3920,
        'Gait Training, Post-surgical Mobility, Spine Rehabilitation, Assistive Device Fitting, Home Therapy',
        500.00, 'Sat - Thu: 9:00 AM - 5:00 PM',
        NULL, NULL, NOW()
    ) RETURNING "Id" INTO v_p3_id;

    -- 4. Physiotherapist (Sylhet)
    INSERT INTO "HealthcareProviders" (
        "Name", "ProviderType", "OrganizationOrClinic", "Description", "Specialization",
        "Phone", "Email", "Address", "City", "District", "Latitude", "Longitude",
        "AvailableServices", "ConsultationFee", "AvailabilitySchedule",
        "ProfileImage", "OrganizationId", "CreatedAt"
    ) VALUES (
        'Nazmul Hasan, BPT', 'Physiotherapist', 'Sylhet Care Physiotherapy',
        'Specialist in sports injury rehabilitation, stroke hemiplegia recovery, and muscle strengthening for persons with physical disabilities.',
        'Neuro-Rehabilitation & Mobility Training',
        '+880 1714 000004', 'nazmul@sylhetcare.bd', 'Zindabazar Point', 'Zindabazar', 'Sylhet',
        24.8949, 91.8687,
        'Stroke Hemiplegia Rehab, Joint Mobilization, Therapeutic Exercise, Pain Management',
        400.00, 'Sun - Fri: 10:00 AM - 6:00 PM',
        NULL, NULL, NOW()
    ) RETURNING "Id" INTO v_p4_id;

    -- 5. Speech Therapist (Dhaka)
    INSERT INTO "HealthcareProviders" (
        "Name", "ProviderType", "OrganizationOrClinic", "Description", "Specialization",
        "Phone", "Email", "Address", "City", "District", "Latitude", "Longitude",
        "AvailableServices", "ConsultationFee", "AvailabilitySchedule",
        "ProfileImage", "OrganizationId", "CreatedAt"
    ) VALUES (
        'Tahmina Akter, MSc (Speech)', 'Speech Therapist', 'Shishu Bikash Speech Center',
        'Experienced speech-language pathologist addressing articulation disorders, stammering, autism communication interventions, and alternative communication tools.',
        'Speech, Language & Swallowing Therapy',
        '+880 1815 000005', 'tahmina@shishubikash.org', 'Level 4, City Heart Building, Nayapaltan', 'Nayapaltan', 'Dhaka',
        23.7340, 90.4120,
        'Autism Communication Therapy, Articulation Therapy, AAC Device Training, Swallowing Assessment',
        600.00, 'Sat - Wed: 10:00 AM - 4:00 PM',
        NULL, NULL, NOW()
    ) RETURNING "Id" INTO v_p5_id;

    -- 6. Rehabilitation Center (Savar, Dhaka)
    INSERT INTO "HealthcareProviders" (
        "Name", "ProviderType", "OrganizationOrClinic", "Description", "Specialization",
        "Phone", "Email", "Address", "City", "District", "Latitude", "Longitude",
        "AvailableServices", "ConsultationFee", "AvailabilitySchedule",
        "ProfileImage", "OrganizationId", "CreatedAt"
    ) VALUES (
        'CRP Savar - Centre for Rehabilitation of the Paralysed', 'Rehabilitation Center', 'CRP Bangladesh',
        'National pioneer center providing holistic multidisciplinary rehabilitation for spinal cord injury, amputation, cerebral palsy, with inpatient and outpatient facilities.',
        'Comprehensive Inpatient & Outpatient Disability Rehabilitation',
        '+880 2 7745464', 'contact@crp-bangladesh.org', 'CRP-Chapain, Savar', 'Savar', 'Dhaka',
        23.8583, 90.2667,
        'Spinal Cord Injury Rehab, Prosthetics & Orthotics, Vocational Training, Hydrotherapy, Day Care',
        300.00, 'Mon - Sat: 8:30 AM - 4:30 PM',
        NULL, NULL, NOW()
    ) RETURNING "Id" INTO v_p6_id;

    -- 7. Eye Specialist (Dhaka)
    INSERT INTO "HealthcareProviders" (
        "Name", "ProviderType", "OrganizationOrClinic", "Description", "Specialization",
        "Phone", "Email", "Address", "City", "District", "Latitude", "Longitude",
        "AvailableServices", "ConsultationFee", "AvailabilitySchedule",
        "ProfileImage", "OrganizationId", "CreatedAt"
    ) VALUES (
        'Dr. Aminul Haque, FCPS (Ophth)', 'Eye Specialist', 'National Institute of Ophthalmology',
        'Ophthalmologist and low vision specialist dedicated to vision rehabilitation, assistive reading lenses, and glaucoma management.',
        'Low Vision Rehabilitation & Retinal Care',
        '+880 1717 000007', 'dr.aminul.eye@nio.gov.bd', 'Sher-e-Bangla Nagar', 'Agargaon', 'Dhaka',
        23.7772, 90.3742,
        'Low Vision Device Assessment, Cataract Surgery, Glaucoma Management, Visual Field Analysis',
        500.00, 'Sun - Thu: 9:00 AM - 2:00 PM',
        NULL, NULL, NOW()
    ) RETURNING "Id" INTO v_p7_id;

    -- 8. Mental Health Specialist (Dhaka)
    INSERT INTO "HealthcareProviders" (
        "Name", "ProviderType", "OrganizationOrClinic", "Description", "Specialization",
        "Phone", "Email", "Address", "City", "District", "Latitude", "Longitude",
        "AvailableServices", "ConsultationFee", "AvailabilitySchedule",
        "ProfileImage", "OrganizationId", "CreatedAt"
    ) VALUES (
        'Syeda Roksana Begum', 'Mental Health Specialist', 'MIND Care Center',
        'Lead clinical psychologist offering mental health counseling, autism behavior support, and caregiver psychological therapy.',
        'Clinical Psychology & Neurodevelopmental Mental Health',
        '+880 1918 000008', 'roksana@mindcare.bd', 'House 12, Road 11, Banani', 'Banani', 'Dhaka',
        23.7925, 90.4078,
        'Cognitive Behavioral Therapy, Neurodevelopmental Assessment, Caregiver Mental Health Counseling, Trauma Support',
        700.00, 'Sat - Wed: 3:00 PM - 8:00 PM',
        NULL, NULL, NOW()
    ) RETURNING "Id" INTO v_p8_id;

    -- 9. Doctor (Uttara, Dhaka)
    INSERT INTO "HealthcareProviders" (
        "Name", "ProviderType", "OrganizationOrClinic", "Description", "Specialization",
        "Phone", "Email", "Address", "City", "District", "Latitude", "Longitude",
        "AvailableServices", "ConsultationFee", "AvailabilitySchedule",
        "ProfileImage", "OrganizationId", "CreatedAt"
    ) VALUES (
        'Dr. Kazi Imran', 'Doctor', 'Care & Cure Medical Center',
        'Physical Medicine and Rehabilitation specialist providing advanced joint care and mobility therapy.',
        'Physical Medicine & Rehabilitation',
        '+880 1719 000009', 'dr.kazi.imran@carecure.bd', 'Sector 7, Uttara', 'Uttara', 'Dhaka',
        23.8759, 90.3795,
        'Joint Pain Management, Musculoskeletal Rehab, Trigger Point Injections',
        600.00, 'Sat - Thu: 5:00 PM - 9:00 PM',
        NULL, NULL, NOW()
    ) RETURNING "Id" INTO v_p9_id;

    -- 10. Completed Appointment for Disability User (to test Review submission eligibility!)
    IF v_user_id IS NOT NULL THEN
        INSERT INTO "HealthcareAppointments" (
            "HealthcareProviderId", "DisabilityUserId", "AppointmentDate", "TimeSlot",
            "ReasonForVisit", "PatientNotes", "Status", "DoctorNotes", "CreatedAt"
        ) VALUES (
            v_p1_id, v_user_id, NOW() - INTERVAL '3 days', '04:00 PM - 04:30 PM',
            'Follow-up evaluation on post-stroke mobility rehabilitation and wheelchair adjustments.',
            'Wheelchair ramp assistance required at entrance.',
            'Completed',
            'Patient mobility is steadily improving. Prescribed continuation of physiotherapy and updated exercise plan.',
            NOW() - INTERVAL '5 days'
        ) RETURNING "Id" INTO v_appt_completed_id;

        -- 11. Initial Review by the patient who had the completed appointment
        INSERT INTO "HealthcareReviews" (
            "HealthcareProviderId", "DisabilityUserId", "Rating", "ReviewText", "CreatedAt"
        ) VALUES (
            v_p1_id, v_user_id, 5,
            'Dr. Mahbubur Rahman and the clinic staff provided outstanding care. The ground floor wheelchair access made entry effortless, and his therapy recommendations have significantly helped my mobility.',
            NOW() - INTERVAL '2 days'
        );

        -- 12. Pending Appointment for Disability User with Physiotherapist (to test appointment management)
        INSERT INTO "HealthcareAppointments" (
            "HealthcareProviderId", "DisabilityUserId", "AppointmentDate", "TimeSlot",
            "ReasonForVisit", "PatientNotes", "Status", "DoctorNotes", "CreatedAt"
        ) VALUES (
            v_p3_id, v_user_id, NOW() + INTERVAL '2 days', '10:00 AM - 10:30 AM',
            'Need physical therapy session for lower back stiffness and gait training.',
            'Requires low-rise treatment table.',
            'Pending',
            NULL,
            NOW()
        );
    END IF;

    RAISE NOTICE 'Healthcare & Therapy Finder seed data successfully populated.';
END $$;
