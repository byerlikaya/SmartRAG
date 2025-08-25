# GitHub Pages Setup Guide

This guide explains how to set up GitHub Pages for your SmartRAG documentation.

## Prerequisites

- A GitHub repository with SmartRAG code
- Admin access to the repository
- Documentation files in the `docs/` folder

## Step 1: Enable GitHub Pages

1. Go to your GitHub repository
2. Click on **Settings** tab
3. Scroll down to **Pages** section (left sidebar)
4. Under **Source**, select **Deploy from a branch**
5. Choose **main** branch and **/docs** folder
6. Click **Save**

## Step 2: Configure GitHub Actions

The repository already includes a GitHub Actions workflow (`.github/workflows/docs.yml`) that will automatically build and deploy your documentation.

### What the workflow does:

- **Triggers**: Runs on pushes to main branch or pull requests
- **Build**: Sets up Ruby, Jekyll, and builds the documentation
- **Deploy**: Automatically deploys to GitHub Pages
- **Security**: Uses proper permissions and security tokens

## Step 3: Verify Deployment

1. After pushing changes to the main branch, the workflow will run automatically
2. Check the **Actions** tab to monitor the build process
3. Once complete, your site will be available at: `https://yourusername.github.io/your-repo-name`

## Step 4: Custom Domain (Optional)

If you want to use a custom domain:

1. In repository **Settings** → **Pages**
2. Enter your custom domain in the **Custom domain** field
3. Add a CNAME record pointing to `yourusername.github.io`
4. Check **Enforce HTTPS** if available

## Step 5: Local Development

To test your documentation locally:

```bash
# Install Ruby and Jekyll
gem install jekyll bundler

# Navigate to docs folder
cd docs

# Install dependencies
bundle install

# Start local server
bundle exec jekyll serve

# Open http://localhost:4000 in your browser
```

## Troubleshooting

### Common Issues

1. **Build fails**: Check the Actions tab for error details
2. **Site not updating**: Wait a few minutes for deployment to complete
3. **Styling issues**: Ensure all CSS and JS files are properly linked
4. **Navigation problems**: Verify `_config.yml` and navigation files

### Jekyll Build Errors

```bash
# Clear Jekyll cache
bundle exec jekyll clean

# Check Jekyll version
jekyll --version

# Update Jekyll
gem update jekyll
```

## File Structure

```
docs/
├── _config.yml          # Jekyll configuration
├── _layouts/            # HTML templates
├── _data/               # Navigation data
├── assets/              # CSS, JS, images
├── index.md             # Home page
├── getting-started.md   # Getting started guide
├── configuration.md     # Configuration guide
├── api-reference.md     # API documentation
├── examples.md          # Usage examples
├── troubleshooting.md   # Troubleshooting guide
└── Gemfile             # Ruby dependencies
```

## Customization

### Theme

The documentation uses the **Just the Docs** theme. To customize:

1. Edit `_config.yml` for theme options
2. Modify `assets/css/style.css` for custom styles
3. Update `_layouts/default.html` for layout changes

### Navigation

Edit `_data/navigation.yml` to modify the navigation structure:

```yaml
- title: Getting Started
  url: /getting-started
  children:
    - title: Installation
      url: /getting-started#installation
```

### Content

- Use Markdown for all content
- Include front matter for Jekyll metadata
- Use proper heading hierarchy (H1, H2, H3)
- Include code examples with syntax highlighting

## Maintenance

### Regular Tasks

1. **Update dependencies**: Run `bundle update` periodically
2. **Check links**: Verify all internal and external links work
3. **Review content**: Keep documentation up to date with code changes
4. **Monitor performance**: Check page load times and user experience

### Version Control

- Commit documentation changes with descriptive messages
- Use pull requests for major documentation updates
- Tag releases to match code versions
- Keep documentation in sync with code changes

## Support

If you encounter issues:

1. Check the [Jekyll documentation](https://jekyllrb.com/docs/)
2. Review [GitHub Pages documentation](https://docs.github.com/en/pages)
3. Search existing issues in the repository
4. Create a new issue with detailed information

## Next Steps

After setting up GitHub Pages:

1. **Customize the theme** to match your brand
2. **Add analytics** (Google Analytics, Plausible, etc.)
3. **Set up search** functionality
4. **Add feedback mechanisms** for users
5. **Create a documentation style guide** for contributors
