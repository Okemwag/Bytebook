using ByteBook.Domain.ValueObjects;

namespace ByteBook.UnitTests.Domain.ValueObjects;

public class UserProfileTests
{
    [Fact]
    public void UserProfile_DefaultConstructor_ShouldCreateEmptyProfile()
    {
        // Act
        var profile = new UserProfile();

        // Assert
        Assert.Null(profile.Bio);
        Assert.Null(profile.Website);
        Assert.Null(profile.TwitterHandle);
        Assert.Null(profile.LinkedInProfile);
        Assert.Null(profile.AvatarUrl);
        Assert.Null(profile.Location);
        Assert.NotNull(profile.Interests);
        Assert.Empty(profile.Interests);
    }

    [Fact]
    public void UserProfile_ParameterizedConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var bio = "Software Developer";
        var website = "https://example.com";
        var twitter = "@johndoe";
        var linkedin = "johndoe";
        var avatar = "https://example.com/avatar.jpg";
        var interests = new List<string> { "Programming", "Reading" };
        var location = "New York";

        // Act
        var profile = new UserProfile(bio, website, twitter, linkedin, avatar, interests, location);

        // Assert
        Assert.Equal(bio, profile.Bio);
        Assert.Equal(website, profile.Website);
        Assert.Equal(twitter, profile.TwitterHandle);
        Assert.Equal(linkedin, profile.LinkedInProfile);
        Assert.Equal(avatar, profile.AvatarUrl);
        Assert.Equal(location, profile.Location);
        Assert.Equal(interests, profile.Interests);
    }

    [Fact]
    public void UserProfile_ParameterizedConstructor_WithNullInterests_ShouldCreateEmptyList()
    {
        // Act
        var profile = new UserProfile("Bio", null, null, null, null, null, null);

        // Assert
        Assert.NotNull(profile.Interests);
        Assert.Empty(profile.Interests);
    }

    [Fact]
    public void UserProfile_UpdateBio_ShouldReturnNewInstanceWithUpdatedBio()
    {
        // Arrange
        var originalProfile = new UserProfile("Old Bio", "website", "twitter", "linkedin", "avatar", new List<string>(), "location");
        var newBio = "New Bio";

        // Act
        var updatedProfile = originalProfile.UpdateBio(newBio);

        // Assert
        Assert.Equal(newBio, updatedProfile.Bio);
        Assert.Equal(originalProfile.Website, updatedProfile.Website);
        Assert.Equal(originalProfile.TwitterHandle, updatedProfile.TwitterHandle);
        Assert.Equal(originalProfile.LinkedInProfile, updatedProfile.LinkedInProfile);
        Assert.Equal(originalProfile.AvatarUrl, updatedProfile.AvatarUrl);
        Assert.Equal(originalProfile.Location, updatedProfile.Location);
        Assert.Equal(originalProfile.Interests, updatedProfile.Interests);
        
        // Original should remain unchanged
        Assert.Equal("Old Bio", originalProfile.Bio);
    }

    [Fact]
    public void UserProfile_UpdateWebsite_ShouldReturnNewInstanceWithUpdatedWebsite()
    {
        // Arrange
        var originalProfile = new UserProfile("bio", "old-website", "twitter", "linkedin", "avatar", new List<string>(), "location");
        var newWebsite = "new-website";

        // Act
        var updatedProfile = originalProfile.UpdateWebsite(newWebsite);

        // Assert
        Assert.Equal(originalProfile.Bio, updatedProfile.Bio);
        Assert.Equal(newWebsite, updatedProfile.Website);
        Assert.Equal(originalProfile.TwitterHandle, updatedProfile.TwitterHandle);
        Assert.Equal(originalProfile.LinkedInProfile, updatedProfile.LinkedInProfile);
        Assert.Equal(originalProfile.AvatarUrl, updatedProfile.AvatarUrl);
        Assert.Equal(originalProfile.Location, updatedProfile.Location);
        Assert.Equal(originalProfile.Interests, updatedProfile.Interests);
    }

    [Fact]
    public void UserProfile_UpdateSocialProfiles_ShouldReturnNewInstanceWithUpdatedSocialProfiles()
    {
        // Arrange
        var originalProfile = new UserProfile("bio", "website", "old-twitter", "old-linkedin", "avatar", new List<string>(), "location");
        var newTwitter = "new-twitter";
        var newLinkedIn = "new-linkedin";

        // Act
        var updatedProfile = originalProfile.UpdateSocialProfiles(newTwitter, newLinkedIn);

        // Assert
        Assert.Equal(originalProfile.Bio, updatedProfile.Bio);
        Assert.Equal(originalProfile.Website, updatedProfile.Website);
        Assert.Equal(newTwitter, updatedProfile.TwitterHandle);
        Assert.Equal(newLinkedIn, updatedProfile.LinkedInProfile);
        Assert.Equal(originalProfile.AvatarUrl, updatedProfile.AvatarUrl);
        Assert.Equal(originalProfile.Location, updatedProfile.Location);
        Assert.Equal(originalProfile.Interests, updatedProfile.Interests);
    }

    [Fact]
    public void UserProfile_UpdateInterests_ShouldReturnNewInstanceWithUpdatedInterests()
    {
        // Arrange
        var originalProfile = new UserProfile("bio", "website", "twitter", "linkedin", "avatar", new List<string> { "Old Interest" }, "location");
        var newInterests = new List<string> { "New Interest 1", "New Interest 2" };

        // Act
        var updatedProfile = originalProfile.UpdateInterests(newInterests);

        // Assert
        Assert.Equal(originalProfile.Bio, updatedProfile.Bio);
        Assert.Equal(originalProfile.Website, updatedProfile.Website);
        Assert.Equal(originalProfile.TwitterHandle, updatedProfile.TwitterHandle);
        Assert.Equal(originalProfile.LinkedInProfile, updatedProfile.LinkedInProfile);
        Assert.Equal(originalProfile.AvatarUrl, updatedProfile.AvatarUrl);
        Assert.Equal(originalProfile.Location, updatedProfile.Location);
        Assert.Equal(newInterests, updatedProfile.Interests);
    }

    [Fact]
    public void UserProfile_EqualityComparison_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var interests = new List<string> { "Programming", "Reading" };
        var profile1 = new UserProfile("Bio", "Website", "Twitter", "LinkedIn", "Avatar", interests, "Location");
        var profile2 = new UserProfile("Bio", "Website", "Twitter", "LinkedIn", "Avatar", interests, "Location");

        // Act & Assert
        Assert.Equal(profile1, profile2);
        Assert.True(profile1.Equals(profile2));
    }

    [Fact]
    public void UserProfile_EqualityComparison_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var interests1 = new List<string> { "Programming", "Reading" };
        var interests2 = new List<string> { "Writing", "Music" };
        var profile1 = new UserProfile("Bio1", "Website", "Twitter", "LinkedIn", "Avatar", interests1, "Location");
        var profile2 = new UserProfile("Bio2", "Website", "Twitter", "LinkedIn", "Avatar", interests2, "Location");

        // Act & Assert
        Assert.NotEqual(profile1, profile2);
        Assert.False(profile1.Equals(profile2));
    }

    [Fact]
    public void UserProfile_EqualityComparison_WithDifferentInterestOrder_ShouldBeEqual()
    {
        // Arrange
        var interests1 = new List<string> { "Programming", "Reading" };
        var interests2 = new List<string> { "Reading", "Programming" };
        var profile1 = new UserProfile("Bio", "Website", "Twitter", "LinkedIn", "Avatar", interests1, "Location");
        var profile2 = new UserProfile("Bio", "Website", "Twitter", "LinkedIn", "Avatar", interests2, "Location");

        // Act & Assert
        Assert.Equal(profile1, profile2);
    }

    [Fact]
    public void UserProfile_EqualityComparison_WithNullValues_ShouldHandleCorrectly()
    {
        // Arrange
        var profile1 = new UserProfile(null, null, null, null, null, null, null);
        var profile2 = new UserProfile(null, null, null, null, null, null, null);

        // Act & Assert
        Assert.Equal(profile1, profile2);
    }
}